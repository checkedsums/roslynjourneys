﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class ConversionsBase
    {
        /// <remarks>
        /// NOTE: Keep this method in sync with <see cref="AnalyzeImplicitUserDefinedConversionForV6SwitchGoverningType"/>.
        /// </remarks>
        private UserDefinedConversionResult AnalyzeImplicitUserDefinedConversions(
            BoundExpression sourceExpression,
            TypeSymbol source,
            TypeSymbol target,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(sourceExpression is null || Compilation is not null);
            Debug.Assert(sourceExpression != null || source is not null);
            Debug.Assert(target is not null);

            // User-defined conversions that involve generics can be quite strange. There
            // are two basic problems: first, that generic user-defined conversions can be
            // "shadowed" by built-in conversions, and second, that generic user-defined
            // conversions can make conversions that would never have been legal user-defined
            // conversions if declared non-generically. I call this latter kind of conversion
            // a "suspicious" conversion.
            //
            // The shadowed conversions are easily dealt with:
            //
            // SPEC: If a predefined implicit conversion exists from a type S to type T,
            // SPEC: all user-defined conversions, implicit or explicit, are ignored.
            // SPEC: If a predefined explicit conversion exists from a type S to type T,
            // SPEC: any user-defined explicit conversion from S to T are ignored.
            //
            // The rule above can come into play in cases like:
            //
            // sealed class C<T> { public static implicit operator T(C<T> c) { ... } }
            // C<object> c = whatever;
            // object o = c;
            //
            // The built-in implicit conversion from C<object> to object must shadow
            // the user-defined implicit conversion.
            //
            // The caller of this method checks for user-defined conversions *after*
            // predefined implicit conversions, so we already know that if we got here,
            // there was no predefined implicit conversion. 
            //
            // Note that a user-defined *implicit* conversion may win over a built-in
            // *explicit* conversion by the rule given above. That is, if we created
            // an implicit conversion from T to C<T>, then the user-defined implicit 
            // conversion from object to C<object> could be valid, even though that
            // would be "replacing" a built-in explicit conversion with a user-defined
            // implicit conversion. This is one of the "suspicious" conversions,
            // as it would not be legal to declare a user-defined conversion from
            // object in a non-generic type.
            //
            // The way the native compiler handles suspicious conversions involving
            // interfaces is neither sensible nor in line with the rules in the 
            // specification. It is not clear at this time whether we should be exactly
            // matching the native compiler, the specification, or neither, in Roslyn.

            // Spec (6.4.4 User-defined implicit conversions)
            //   A user-defined implicit conversion from an expression E to type T is processed as follows:

            // SPEC: Find the set of types D from which user-defined conversion operators...
            var d = ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol ConstrainedToTypeOpt)>.GetInstance();
            AddTypesParticipatingInUserDefinedConversion(d, source, target, ref useSiteInfo);

            // SPEC: Find the set of applicable user-defined and lifted conversion operators, U...
            var ubuild = ArrayBuilder<UserDefinedConversionAnalysis>.GetInstance();
            ComputeApplicableUserDefinedImplicitConversionSet(sourceExpression, source, target, d, ubuild, ref useSiteInfo);
            d.Free();
            ImmutableArray<UserDefinedConversionAnalysis> u = ubuild.ToImmutableAndFree();

            // SPEC: If U is empty, the conversion is undefined and a compile-time error occurs.
            if (u.Length == 0)
            {
                return UserDefinedConversionResult.NoApplicableOperators(u);
            }

            // SPEC: Find the most specific source type SX of the operators in U...
            TypeSymbol sx = MostSpecificSourceTypeForImplicitUserDefinedConversion(u, source, ref useSiteInfo);
            if (sx is null)
            {
                return UserDefinedConversionResult.NoBestSourceType(u);
            }

            // SPEC: Find the most specific target type TX of the operators in U...
            TypeSymbol tx = MostSpecificTargetTypeForImplicitUserDefinedConversion(u, target, ref useSiteInfo);
            if (tx is null)
            {
                return UserDefinedConversionResult.NoBestTargetType(u);
            }

            int? best = MostSpecificConversionOperator(sx, tx, u);
            if (best == null)
            {
                return UserDefinedConversionResult.Ambiguous(u);
            }

            return UserDefinedConversionResult.Valid(u, best.Value);
        }

        /// <summary>
        /// This method find the set of applicable user-defined and lifted conversion operators, u.
        /// The set consists of the user-defined and lifted implicit conversion operators declared by
        /// the classes and structs in d that convert from a type encompassing source to a type encompassed by target.
        /// However if allowAnyTarget is true, then it considers all operators that convert from a type encompassing source
        /// to any target. This flag must be set only if we are computing user defined conversions from a given source
        /// type to any target type.
        /// </summary>
        /// <remarks>
        /// Currently allowAnyTarget flag is only set to true by <see cref="AnalyzeImplicitUserDefinedConversionForV6SwitchGoverningType"/>,
        /// where we must consider user defined implicit conversions from the type of the switch expression to
        /// any of the possible switch governing types.
        /// </remarks>
        private void ComputeApplicableUserDefinedImplicitConversionSet(
            BoundExpression sourceExpression,
            TypeSymbol source,
            TypeSymbol target,
            ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol ConstrainedToTypeOpt)> d,
            ArrayBuilder<UserDefinedConversionAnalysis> u,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            bool allowAnyTarget = false)
        {
            Debug.Assert(sourceExpression is null || Compilation is not null);
            Debug.Assert(sourceExpression != null || source is not null);
            Debug.Assert((target is not null) == !allowAnyTarget);
            Debug.Assert(d != null);
            Debug.Assert(u != null);

            // SPEC: Find the set of applicable user-defined and lifted conversion operators, U.
            // SPEC: The set consists of the user-defined and lifted implicit conversion operators
            // SPEC: declared by the classes and structs in D that convert from a type encompassing
            // SPEC: E to a type encompassed by T. If U is empty, the conversion is undefined and
            // SPEC: a compile-time error occurs.

            // SPEC: Give a user-defined conversion operator that converts from a non-nullable
            // SPEC: value type S to a non-nullable value type T, a lifted conversion operator
            // SPEC: exists that converts from S? to T?.

            // DELIBERATE SPEC VIOLATION:
            //
            // The spec here essentially says that we add an applicable "regular" conversion and 
            // an applicable lifted conversion, if there is one, to the candidate set, and then
            // let them duke it out to determine which one is "best".
            //
            // This is not at all what the native compiler does, and attempting to implement
            // the specification, or slight variations on it, produces too many backwards-compatibility
            // breaking changes.
            //
            // The native compiler deviates from the specification in two major ways here.
            // First, it does not add *both* the regular and lifted forms to the candidate set.
            // Second, the way it characterizes a "lifted" form is very, very different from
            // how the specification characterizes a lifted form. 
            //
            // An operation, in this case, X-->Y, is properly said to be "lifted" to X?-->Y? via
            // the rule that X?-->Y? matches the behavior of X-->Y for non-null X, and converts
            // null X to null Y otherwise.
            //
            // The native compiler, by contrast, takes the existing operator and "lifts" either
            // the operator's parameter type or the operator's return type to nullable. For
            // example, a conversion from X?-->Y would be "lifted" to X?-->Y? by making the
            // conversion from X? to Y, and then from Y to Y?.  No "lifting" semantics
            // are imposed; we do not check to see if the X? is null. This operator is not
            // actually "lifted" at all; rather, an implicit conversion is applied to the 
            // output. **The native compiler considers the result type Y? of that standard implicit
            // conversion to be the result type of the "lifted" conversion**, rather than
            // properly considering Y to be the result type of the conversion for the purposes 
            // of computing the best output type.
            //
            // MOREOVER: the native compiler actually *does* implement nullable lifting semantics
            // in the case where the input type of the user-defined conversion is a non-nullable
            // value type and the output type is a nullable value type **or pointer type, or 
            // reference type**. This is an enormous departure from the specification; the
            // native compiler will take a user-defined conversion from X-->Y? or X-->C and "lift"
            // it to a conversion from X?-->Y? or X?-->C that has nullable semantics.
            // 
            // This is quite confusing. In this code we will classify the conversion as either
            // "normal" or "lifted" on the basis of *whether or not special lifting semantics
            // are to be applied*. That is, whether or not a later rewriting pass is going to
            // need to insert a check to see if the source expression is null, and decide
            // whether or not to call the underlying unlifted conversion or produce a null
            // value without calling the unlifted conversion.

            // DELIBERATE SPEC VIOLATION (See bug 17021)
            // The specification defines a type U as "encompassing" a type V
            // if there is a standard implicit conversion from U to V, and
            // neither are interface types.
            //
            // The intention of this language is to ensure that we do not allow user-defined
            // conversions that involve interfaces. We have a reasonable expectation that a
            // conversion that involves an interface is one that preserves referential identity,
            // and user-defined conversions usually do not.
            //
            // Now, suppose we have a standard conversion from Alpha to Beta, a user-defined
            // conversion from Beta to Gamma, and a standard conversion from Gamma to Delta.
            // The specification allows the implicit conversion from Alpha to Delta only if 
            // Beta encompasses Alpha and Delta encompasses Gamma.  And therefore, none of them
            // can be interface types, de jure.
            //
            // However, the dev10 compiler only checks Alpha and Delta to see if they are interfaces,
            // and allows Beta and Gamma to be interfaces. 
            //
            // So what's the big deal there? It's not legal to define a user-defined conversion where
            // the input or output types are interfaces, right?
            //
            // It is not legal to define such a conversion, no, but it is legal to create one via generic
            // construction. If we have a conversion from T to C<T>, then C<I> has a conversion from I to C<I>.
            //
            // The dev10 compiler fails to check for this situation. This means that, 
            // you can convert from int to C<IComparable> because int implements IComparable, but cannot
            // convert from IComparable to C<IComparable>!
            //
            // Unfortunately, we know of several real programs that rely upon this bug, so we are going
            // to reproduce it here.

            if (source is not null && source.IsInterfaceType() || target is not null && target.IsInterfaceType())
            {
                return;
            }

            if (IgnoreUserDefinedSpanConversions(source, target))
            {
                return;
            }

            bool haveInterfaces = false;

            foreach ((NamedTypeSymbol declaringType, TypeParameterSymbol constrainedToTypeOpt) in d)
            {
                if (declaringType.IsInterface)
                {
                    Debug.Assert(constrainedToTypeOpt is not null);
                    haveInterfaces = true;
                }
                else
                {
                    addCandidatesFromType(constrainedToTypeOpt: null, declaringType, sourceExpression, source, target, u, ref useSiteInfo, allowAnyTarget);
                }
            }

            if (u.Count == 0 && haveInterfaces)
            {
                foreach ((NamedTypeSymbol declaringType, TypeParameterSymbol constrainedToTypeOpt) in d)
                {
                    if (declaringType.IsInterface)
                    {
                        addCandidatesFromType(constrainedToTypeOpt: constrainedToTypeOpt, declaringType, sourceExpression, source, target, u, ref useSiteInfo, allowAnyTarget);
                    }
                }
            }

            void addCandidatesFromType(
                TypeParameterSymbol constrainedToTypeOpt,
                NamedTypeSymbol declaringType,
                BoundExpression sourceExpression,
                TypeSymbol source,
                TypeSymbol target,
                ArrayBuilder<UserDefinedConversionAnalysis> u,
                ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
                bool allowAnyTarget)
            {
                var operators = ArrayBuilder<MethodSymbol>.GetInstance();
                declaringType.AddOperators(WellKnownMemberNames.ImplicitConversionName, operators);

                if (source._sourceUserDefinedOperators is not null)
                    operators.AddRange(source._sourceUserDefinedOperators);
                if (target._sourceUserDefinedOperators is not null)
                    operators.AddRange(target._sourceUserDefinedOperators);

Goto:;

                for (var i = 0; i < operators.Count; i++)
                {
                    for (var j = 0; j < operators.Count; j++)
                    {
                        if (i == j) j++;

                        if (i < operators.Count && j < operators.Count)
                        {
                            var b = operators[j] as SourceUserDefinedConversionSymbol;
                            var d = operators[i] as SourceUserDefinedConversionSymbol;

                            if (d?.ReturnType is null || d?.ContainingType is null)
                                operators.RemoveAt(j);

                            try
                            {
                                if (b?.ReturnType is null || b?.ContainingType is null ||
                                (b.ReturnType.Equals(d.ReturnType) && b.ContainingType.ToDisplayString() == d.ContainingType.ToDisplayString()))
                                {
                                    operators.RemoveAt(i);
                                    goto Goto;
                                }
                            }
                            catch
                            {
                                operators.RemoveAt(i);
                            }
                        }
                    }
                }

                foreach (MethodSymbol op in operators)
                {
                    for (var i = 0; i < u.Count; i++)
                    {
                        var b = u[i].Operator as SourceUserDefinedConversionSymbol;

                        if (b?.ReturnType is null || b?.ContainingType is null || (b.ReturnType.Equals(op.ReturnType) && b.ContainingType.ToDisplayString() == op.ContainingType.ToDisplayString()))
                            goto Continue;
                    }

                    goto Skip; Continue: continue; Skip:;

                    // We might have a bad operator and be in an error recovery situation. Ignore it.
                    if (op.ReturnsVoid || op.ParameterCount != 1)
                    {
                        continue;
                    }

                    TypeSymbol convertsFrom = op.GetParameterType(0);
                    TypeSymbol convertsTo = op.ReturnType;
                    Conversion fromConversion = EncompassingImplicitConversion(sourceExpression, source, convertsFrom, ref useSiteInfo);
                    Conversion toConversion = allowAnyTarget ? Conversion.Identity :
                        EncompassingImplicitConversion(convertsTo, target, ref useSiteInfo);

                    if (fromConversion.Exists && toConversion.Exists)
                    {
                        // There is an additional spec violation in the native compiler. Suppose
                        // we have a conversion from X-->Y and are asked to do "Y? y = new X();"  Clearly
                        // the intention is to convert from X-->Y via the implicit conversion, and then
                        // stick a standard implicit conversion from Y-->Y? on the back end. **In this 
                        // situation, the native compiler treats the conversion as though it were
                        // actually X-->Y? in source for the purposes of determining the best target
                        // type of an operator.
                        //
                        // We perpetuate this fiction here, except for cases when Y is not a valid type
                        // argument for Nullable<T>. This scenario should only be possible when the corlib
                        // defines a type such as int or long to be a ref struct (see
                        // LiftedConversion_InvalidTypeArgument02).

                        if (target is not null && target.IsNullableType() && convertsTo.IsValidNullableTypeArgument())
                        {
                            convertsTo = MakeNullableType(convertsTo);
                            toConversion = allowAnyTarget ? Conversion.Identity :
                                EncompassingImplicitConversion(convertsTo, target, ref useSiteInfo);
                        }

                        u.Add(UserDefinedConversionAnalysis.Normal(constrainedToTypeOpt, op, fromConversion, toConversion, convertsFrom, convertsTo));
                    }
                    else if (source is not null && source.IsNullableType() && convertsFrom.IsValidNullableTypeArgument() &&
                        (allowAnyTarget || target.CanBeAssignedNull()))
                    {
                        // As mentioned above, here we diverge from the specification, in two ways.
                        // First, we only check for the lifted form if the normal form was inapplicable.
                        // Second, we are supposed to apply lifting semantics only if the conversion 
                        // parameter and return types are *both* non-nullable value types.
                        //
                        // In fact the native compiler determines whether to check for a lifted form on
                        // the basis of:
                        //
                        // * Is the type we are ultimately converting from a nullable value type?
                        // * Is the parameter type of the conversion a non-nullable value type?
                        // * Is the type we are ultimately converting to a nullable value type, 
                        //   pointer type, or reference type?
                        //
                        // If the answer to all those questions is "yes" then we lift to nullable
                        // and see if the resulting operator is applicable.
                        TypeSymbol nullableFrom = MakeNullableType(convertsFrom);
                        TypeSymbol nullableTo = convertsTo.IsValidNullableTypeArgument() ? MakeNullableType(convertsTo) : convertsTo;
                        Conversion liftedFromConversion = EncompassingImplicitConversion(sourceExpression, source, nullableFrom, ref useSiteInfo);
                        Conversion liftedToConversion = !allowAnyTarget ?
                            EncompassingImplicitConversion(nullableTo, target, ref useSiteInfo) :
                            Conversion.Identity;
                        if (liftedFromConversion.Exists && liftedToConversion.Exists)
                        {
                            u.Add(UserDefinedConversionAnalysis.Lifted(constrainedToTypeOpt, op, liftedFromConversion, liftedToConversion, nullableFrom, nullableTo));
                        }
                    }
                }

                operators.Free();
            }
        }

        private TypeSymbol MostSpecificSourceTypeForImplicitUserDefinedConversion(ImmutableArray<UserDefinedConversionAnalysis> u, TypeSymbol source, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // SPEC: If any of the operators in U convert from S then SX is S.
            if (source is not null)
            {
                if (u.Any(static (conv, source) => TypeSymbol.Equals(conv.FromType, source, TypeCompareKind.ConsiderEverything2), source))
                {
                    return source;
                }
            }

            // SPEC: Otherwise, SX is the most encompassed type in the set of
            // SPEC: source types of the operators in U.
            return MostEncompassedType(u, conv => conv.FromType, ref useSiteInfo);
        }

        private TypeSymbol MostSpecificTargetTypeForImplicitUserDefinedConversion(ImmutableArray<UserDefinedConversionAnalysis> u, TypeSymbol target, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // SPEC: If any of the operators in U convert to T then TX is T.
            // SPEC: Otherwise, TX is the most encompassing type in the set of
            // SPEC: target types of the operators in U. 

            // DELIBERATE SPEC VIOLATION:
            // The native compiler deviates from the specification in the way it 
            // determines what the "converts to" type is. The specification is pretty
            // clear that the "converts to" type is the actual return type of the 
            // conversion operator, or, in the case of a lifted operator, the lifted-to-
            // nullable type. That is, if we have X-->Y then the converts-to type of
            // the operator in its normal form is Y, and the converts-to type of the 
            // operator in its lifted form is Y?. 
            //
            // The native compiler does not do this. Suppose we have a user-defined
            // conversion X-->Y, and the assignment Y? y = new X(); -- the native 
            // compiler will consider the converts-to type of X-->Y to be Y?, surprisingly
            // enough. 
            //
            // We have previously written the appropriate "ToType" into the conversion analysis
            // to perpetuate this fiction.

            if (u.Any(static (conv, target) => TypeSymbol.Equals(conv.ToType, target, TypeCompareKind.ConsiderEverything2), target))
            {
                return target;
            }

            return MostEncompassingType(u, conv => conv.ToType, ref useSiteInfo);
        }

        private static int LiftingCount(UserDefinedConversionAnalysis conv)
        {
            int count = 0;
            if (!TypeSymbol.Equals(conv.FromType, conv.Operator.GetParameterType(0), TypeCompareKind.ConsiderEverything2))
            {
                count += 1;
            }

            if (!TypeSymbol.Equals(conv.ToType, conv.Operator.ReturnType, TypeCompareKind.ConsiderEverything2))
            {
                count += 1;
            }

            return count;
        }

        private static int? MostSpecificConversionOperator(TypeSymbol sx, TypeSymbol tx, ImmutableArray<UserDefinedConversionAnalysis> u)
        {
            return MostSpecificConversionOperator(conv => TypeSymbol.Equals(conv.FromType, sx, TypeCompareKind.ConsiderEverything2) && TypeSymbol.Equals(conv.ToType, tx, TypeCompareKind.ConsiderEverything2), u);
        }

        /// <summary>
        /// Find the most specific among a set of conversion operators, with the given constraint on the conversion.
        /// </summary>
        private static int? MostSpecificConversionOperator(Func<UserDefinedConversionAnalysis, bool> constraint, ImmutableArray<UserDefinedConversionAnalysis> u)
        {
            // SPEC: If U contains exactly one user-defined conversion operator from SX to TX 
            // SPEC: then that is the most-specific conversion operator;
            //
            // SPEC: Otherwise, if U contains exactly one lifted conversion operator that converts from
            // SPEC: SX to TX then this is the most specific operator.
            //
            // SPEC: Otherwise, the conversion is ambiguous and a compile-time error occurs.
            //
            // SPEC ERROR:
            //
            // Clearly the text above cannot be correct because it gives undesirable results.
            // Suppose we have structs E and F with an implicit user defined conversion from 
            // F to E. We have an assignment from F to E?. Clearly what should happen is
            // we should convert F to E, then convert E to E?.  But the spec says that this
            // should be an error. Why? Because both F-->E and F?-->E? are added to the candidate
            // set. What is SX? Clearly F, because there is a candidate that takes an F.  
            // What is TX? Clearly E? because there is a candidate that returns an E?.  
            // And now the overload resolution problem is ambiguous because neither operator
            // takes SX and returns TX. 
            //
            // DELIBERATE SPEC VIOLATION:
            //
            // The native compiler takes a rather different approach than the approach described
            // in the specification. Rather than adding both the lifted and unlifted forms of
            // each operator to the candidate set, using those operators to determine the best
            // source and target types, and then choosing the unique operator from that source type
            // to that target type, it instead *transforms in place* the "from" and "to" types
            // of each operator so that their nullability matches those of the source and target
            // types. This can then lead to ambiguities; consider for example a type that
            // has user defined conversions X-->Y and X-->Y?.  If we have a conversion from X to
            // Y?, the spec would say that the operators X-->Y, its lifted form X?-->Y?, and
            // X-->Y? are applicable candidates and that the best of them is X-->Y?.  
            //
            // The native compiler arrives at the same conclusion but by different logic; it says
            // that X-->Y has a "half lifted" form X-->Y?, and that it is "worse" than X-->Y?
            // because it is half lifted.

            // Therefore we match this behavior by first checking to see if there is a unique
            // best operator that converts from the source type to the target type with liftings
            // on neither side.

            BestIndex bestUnlifted = UniqueIndex(u,
                conv =>
                constraint(conv) &&
                LiftingCount(conv) == 0);

            if (bestUnlifted.Kind == BestIndexKind.Best)
            {
                return bestUnlifted.Best;
            }
            else if (bestUnlifted.Kind == BestIndexKind.Ambiguous)
            {
                // If we got an ambiguity, don't continue. We need to bail immediately.

                // UNDONE: We can do better error reporting if we return the ambiguity and
                // use that in the error message.
                return null;
            }

            // There was no fully-unlifted operator. Check to see if there was any *half-lifted* operator. 
            //
            // For example, suppose we had a conversion from X-->Y?, and lifted it to X?-->Y?. (The spec
            // says not to do such a lifting because Y? is not a non-nullable value type, but the native
            // compiler does so and we are being compatible with it.) That would be a half-lifted operator.
            //
            // For example, suppose we had a conversion from X-->Y, and the assignment Y? y = new X(); --
            // this would also be a "half lifted" conversion even though there is no "lifting" going on
            // (in the sense that we are not checking the source to see if it is null.)
            // 

            BestIndex bestHalfLifted = UniqueIndex(u,
                conv =>
                constraint(conv) &&
                LiftingCount(conv) == 1);

            if (bestHalfLifted.Kind == BestIndexKind.Best)
            {
                return bestHalfLifted.Best;
            }
            else if (bestHalfLifted.Kind == BestIndexKind.Ambiguous)
            {
                // UNDONE: We can do better error reporting if we return the ambiguity and
                // use that in the error message.
                return null;
            }

            // Finally, see if there is a unique best *fully lifted* operator.

            BestIndex bestFullyLifted = UniqueIndex(u,
                conv =>
                constraint(conv) &&
                LiftingCount(conv) == 2);

            if (bestFullyLifted.Kind == BestIndexKind.Best)
            {
                return bestFullyLifted.Best;
            }
            else if (bestFullyLifted.Kind == BestIndexKind.Ambiguous)
            {
                // UNDONE: We can do better error reporting if we return the ambiguity and
                // use that in the error message.
                return null;
            }

            return null;
        }

        // Return the index of the *unique* item in the array that matches the predicate,
        // or null if there is not one.
        private static BestIndex UniqueIndex<T>(ImmutableArray<T> items, Func<T, bool> predicate)
        {
            if (items.IsEmpty)
            {
                return BestIndex.None();
            }

            int? result = null;
            for (int i = 0; i < items.Length; ++i)
            {
                if (predicate(items[i]))
                {
                    if (result == null)
                    {
                        result = i;
                    }
                    else
                    {
                        // Not unique.
                        return BestIndex.IsAmbiguous(result.Value, i);
                    }
                }
            }

            return result == null ? BestIndex.None() : BestIndex.HasBest(result.Value);
        }

        // Is A encompassed by B?
        private bool IsEncompassedBy(BoundExpression aExpr, TypeSymbol a, TypeSymbol b, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(aExpr is null || Compilation is not null);
            Debug.Assert(a is not null);
            Debug.Assert(b is not null);

            // SPEC: If a standard implicit conversion exists from a type A to a type B
            // SPEC: and if neither A nor B is an interface type then A is said to be
            // SPEC: encompassed by B, and B is said to encompass A.

            return EncompassingImplicitConversion(aExpr, a, b, ref useSiteInfo).Exists;
        }

        // Is A encompassed by B?
        private bool IsEncompassedBy(TypeSymbol a, TypeSymbol b, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return IsEncompassedBy(aExpr: null, a, b, ref useSiteInfo);
        }

        private Conversion EncompassingImplicitConversion(BoundExpression aExpr, TypeSymbol a, TypeSymbol b, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(aExpr is null || Compilation is not null);
            Debug.Assert(aExpr != null || a is not null);
            Debug.Assert(b is not null);

            // DELIBERATE SPEC VIOLATION: 
            // We ought to be saying that an encompassing conversion never exists when one of
            // the types is an interface type, but due to a desire to be compatible with a 
            // dev10 bug, we allow it. See the comment regarding bug 17021 above for more details.

            var result = ClassifyStandardImplicitConversion(aExpr, a, b, ref useSiteInfo);
            return IsEncompassingImplicitConversionKind(result.Kind) ? result : Conversion.NoConversion;
        }

        private Conversion EncompassingImplicitConversion(TypeSymbol a, TypeSymbol b, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return EncompassingImplicitConversion(aExpr: null, a, b, ref useSiteInfo);
        }

        private static bool IsEncompassingImplicitConversionKind(ConversionKind kind)
        {
            return kind switch
            {
                // Doesn't even exist.
                ConversionKind.NoConversion or ConversionKind.ImplicitDynamic or ConversionKind.MethodGroup or ConversionKind.AnonymousFunction or ConversionKind.InterpolatedString or ConversionKind.SwitchExpression or ConversionKind.ConditionalExpression or ConversionKind.ImplicitEnumeration or ConversionKind.StackAllocToPointerType or ConversionKind.StackAllocToSpanType or ConversionKind.InterpolatedStringHandler or ConversionKind.ImplicitUserDefined or ConversionKind.ExplicitUserDefined or ConversionKind.FunctionType or ConversionKind.ExplicitNumeric or ConversionKind.ExplicitEnumeration or ConversionKind.ExplicitNullable or ConversionKind.ExplicitReference or ConversionKind.Unboxing or ConversionKind.ExplicitDynamic or ConversionKind.ExplicitPointerToPointer or ConversionKind.ExplicitPointerToInteger or ConversionKind.ExplicitIntegerToPointer or ConversionKind.IntPtr or ConversionKind.ExplicitTupleLiteral or ConversionKind.ExplicitTuple or ConversionKind.ExplicitSpan => false,
                // Spec'd in C# 4.
                ConversionKind.Identity or ConversionKind.ImplicitNumeric or ConversionKind.ImplicitNullable or ConversionKind.ImplicitReference or ConversionKind.Boxing or ConversionKind.ImplicitConstant or ConversionKind.ImplicitPointerToVoid or ConversionKind.NullLiteral or ConversionKind.ImplicitNullToPointer or ConversionKind.ImplicitTupleLiteral or ConversionKind.ImplicitTuple or ConversionKind.ImplicitThrow or ConversionKind.DefaultLiteral or ConversionKind.ImplicitPointer or ConversionKind.InlineArray or ConversionKind.ImplicitSpan => true,
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }

        private TypeSymbol MostEncompassedType<T>(
            ImmutableArray<T> items,
            Func<T, TypeSymbol> extract,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return MostEncompassedType<T>(items, x => true, extract, ref useSiteInfo);
        }

        private TypeSymbol MostEncompassedType<T>(
            ImmutableArray<T> items,
            Func<T, bool> valid,
            Func<T, TypeSymbol> extract,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // SPEC: The most encompassed type is the one type in the set that 
            // SPEC: is encompassed by all the other types.

            // We have a bit of a graph theory problem here. Suppose hypothetically
            // speaking we have three types in the set such that:
            //
            // X is encompassed by Y
            // X is encompassed by Z
            // Y is encompassed by X
            //
            // In that situation, X is the unique type in the set that is encompassed
            // by all the other types, despite the fact that it appears to be neither
            // better nor worse than Y! 
            //
            // But in practice this situation never arises because implicit convertibility
            // is transitive; if Y is implicitly convertible to X and X is implicitly convertible
            // to Z, then Y is implicitly convertible to Z.
            //
            // Because we have this transitivity, we can rephrase the problem as follows:
            //
            // Find the unique best type in the set, where the best type is the type that is 
            // better than every other type. By "X is better than Y" we mean "X is encompassed 
            // by Y but Y is not encompassed by X".

            CompoundUseSiteInfo<AssemblySymbol> inLambdaUseSiteInfo = useSiteInfo;
            int? best = UniqueBestValidIndex(items, valid,
                (left, right) =>
                {
                    TypeSymbol leftType = extract(left);
                    TypeSymbol rightType = extract(right);
                    if (TypeSymbol.Equals(leftType, rightType, TypeCompareKind.ConsiderEverything2))
                    {
                        return BetterResult.Equal;
                    }

                    bool leftWins = IsEncompassedBy(leftType, rightType, ref inLambdaUseSiteInfo);
                    bool rightWins = IsEncompassedBy(rightType, leftType, ref inLambdaUseSiteInfo);
                    if (leftWins == rightWins)
                    {
                        return BetterResult.Neither;
                    }
                    return leftWins ? BetterResult.Left : BetterResult.Right;
                });

            useSiteInfo = inLambdaUseSiteInfo;
            return best == null ? null : extract(items[best.Value]);
        }

        private TypeSymbol MostEncompassingType<T>(
            ImmutableArray<T> items,
            Func<T, TypeSymbol> extract,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return MostEncompassingType<T>(items, x => true, extract, ref useSiteInfo);
        }

        private TypeSymbol MostEncompassingType<T>(
            ImmutableArray<T> items,
            Func<T, bool> valid,
            Func<T, TypeSymbol> extract,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // See comments above.
            CompoundUseSiteInfo<AssemblySymbol> inLambdaUseSiteInfo = useSiteInfo;
            int? best = UniqueBestValidIndex(items, valid,
                (left, right) =>
                {
                    TypeSymbol leftType = extract(left);
                    TypeSymbol rightType = extract(right);
                    if (TypeSymbol.Equals(leftType, rightType, TypeCompareKind.ConsiderEverything2))
                    {
                        return BetterResult.Equal;
                    }

                    bool leftWins = IsEncompassedBy(rightType, leftType, ref inLambdaUseSiteInfo);
                    bool rightWins = IsEncompassedBy(leftType, rightType, ref inLambdaUseSiteInfo);
                    if (leftWins == rightWins)
                    {
                        return BetterResult.Neither;
                    }
                    return leftWins ? BetterResult.Left : BetterResult.Right;
                });

            useSiteInfo = inLambdaUseSiteInfo;
            return best == null ? null : extract(items[best.Value]);
        }

        // This method takes an array of items and a predicate which filters out the valid items.
        // From the valid items we find the index of the *unique best item* in the array.
        // In order for a valid item x to be considered best, x must be better than every other
        // item. The "better" relation must be consistent; that is:
        //
        // better(x,y) == Left     requires that    better(y,x) == Right
        // better(x,y) == Right    requires that    better(y,x) == Left
        // better(x,y) == Neither  requires that    better(y,x) == Neither 
        //
        // It is possible for the array to contain the same item twice; if it does then
        // the duplicate is ignored. That is, having the "best" item twice does not preclude
        // it from being the best.

        // UNDONE: Update this to give a BestIndex result that indicates ambiguity.
        private static int? UniqueBestValidIndex<T>(ImmutableArray<T> items, Func<T, bool> valid, Func<T, T, BetterResult> better)
        {
            if (items.IsEmpty)
            {
                return null;
            }

            int? candidateIndex = null;
            T candidateItem = default;

            for (int currentIndex = 0; currentIndex < items.Length; ++currentIndex)
            {
                T currentItem = items[currentIndex];
                if (!valid(currentItem))
                {
                    continue;
                }

                if (candidateIndex == null)
                {
                    candidateIndex = currentIndex;
                    candidateItem = currentItem;
                    continue;
                }

                BetterResult result = better(candidateItem, currentItem);

                if (result == BetterResult.Equal)
                {
                    // The list had the same item twice. Just ignore it.
                    continue;
                }
                else if (result == BetterResult.Neither)
                {
                    // Neither the current item nor the candidate item are better,
                    // and therefore neither of them can be the best. We no longer
                    // have a candidate for best item.
                    candidateIndex = null;
                    candidateItem = default;
                }
                else if (result == BetterResult.Right)
                {
                    // The candidate is worse than the current item, so replace it
                    // with the current item.
                    candidateIndex = currentIndex;
                    candidateItem = currentItem;
                }
                // Otherwise, the candidate is better than the current item, so
                // it continues to be the candidate.
            }

            if (candidateIndex == null)
            {
                return null;
            }

            // We had a candidate that was better than everything that came *after* it.
            // Now verify that it was better than everything that came before it.

            for (int currentIndex = 0; currentIndex < candidateIndex.Value; ++currentIndex)
            {
                T currentItem = items[currentIndex];
                if (!valid(currentItem))
                {
                    continue;
                }

                BetterResult result = better(candidateItem, currentItem);
                if (result != BetterResult.Left && result != BetterResult.Equal)
                {
                    // The candidate was not better than everything that came before it. There is 
                    // no best item.
                    return null;
                }
            }

            // The candidate was better than everything that came before it.

            return candidateIndex;
        }

        private NamedTypeSymbol MakeNullableType(TypeSymbol type)
        {
            var nullable = this.corLibrary.GetDeclaredSpecialType(SpecialType.System_Nullable_T);
            return nullable.Construct(type);
        }

        /// <remarks>
        /// NOTE: Keep this method in sync with AnalyzeImplicitUserDefinedConversion.
        /// </remarks>
        protected UserDefinedConversionResult AnalyzeImplicitUserDefinedConversionForV6SwitchGoverningType(TypeSymbol source, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // SPEC:    The governing type of a switch statement is established by the switch expression.
            // SPEC:    1) If the type of the switch expression is sbyte, byte, short, ushort, int, uint,
            // SPEC:       long, ulong, bool, char, string, or an enum-type, or if it is the nullable type
            // SPEC:       corresponding to one of these types, then that is the governing type of the switch statement. 
            // SPEC:    2) Otherwise, exactly one user-defined implicit conversion (§6.4) must exist from the
            // SPEC:       type of the switch expression to one of the following possible governing types:
            // SPEC:       sbyte, byte, short, ushort, int, uint, long, ulong, char, string, or, a nullable type
            // SPEC:       corresponding to one of those types

            // NOTE:    This method implements part (2) above, it should be called only if (1) is false for source type.
            Debug.Assert(source is not null);
            Debug.Assert(!source.IsValidV6SwitchGoverningType());

            // NOTE: For (2) we use an approach similar to native compiler's approach, but call into the common code for analyzing user defined implicit conversions.
            // NOTE:    (a) Compute the set of types D from which user-defined conversion operators should be considered by considering only the source type.
            // NOTE:    (b) Instead of computing applicable user defined implicit conversions U from the source type to a specific target type,
            // NOTE:        we compute these from the source type to ANY target type.
            // NOTE:    (c) From the conversions in U, select the most specific of them that targets a valid switch governing type

            // SPEC VIOLATION: Because we use the same strategy for computing the most specific conversion, as the Dev10 compiler did (in fact
            // SPEC VIOLATION: we share the code), we inherit any spec deviances in that analysis. Specifically, the analysis only considers
            // SPEC VIOLATION: which conversion has the least amount of lifting, where a conversion may be considered to be in unlifted form,
            // SPEC VIOLATION: half-lifted form (only the argument type or return type is lifted) or fully lifted form. The most specific computation
            // SPEC VIOLATION: looks for a unique conversion that is least lifted. The spec, on the other hand, requires that the conversion
            // SPEC VIOLATION: be *unique*, not merely most use the least amount of lifting among the applicable conversions.

            // SPEC VIOLATION: This introduces a SPEC VIOLATION for the following tests in the native compiler:

            // NOTE:    // See test SwitchTests.CS0166_AggregateTypeWithMultipleImplicitConversions_07
            // NOTE:    struct Conv
            // NOTE:    {
            // NOTE:        public static implicit operator int (Conv C) { return 1; }
            // NOTE:        public static implicit operator int (Conv? C2) { return 0; }
            // NOTE:        public static int Main()
            // NOTE:        {
            // NOTE:            Conv? D = new Conv();
            // NOTE:            switch(D)
            // NOTE:            {   ...

            // SPEC VIOLATION: Native compiler allows the above code to compile
            // SPEC VIOLATION: even though there are two user-defined implicit conversions:
            // SPEC VIOLATION: 1) To int type (applicable in normal form): public static implicit operator int (Conv? C2)
            // SPEC VIOLATION: 2) To int? type (applicable in lifted form): public static implicit operator int (Conv C)

            // NOTE:    // See also test SwitchTests.TODO
            // NOTE:    struct Conv
            // NOTE:    {
            // NOTE:        public static implicit operator int? (Conv C) { return 1; }
            // NOTE:        public static implicit operator string (Conv? C2) { return 0; }
            // NOTE:        public static int Main()
            // NOTE:        {
            // NOTE:            Conv? D = new Conv();
            // NOTE:            switch(D)
            // NOTE:            {   ...

            // SPEC VIOLATION: Native compiler allows the above code to compile too
            // SPEC VIOLATION: even though there are two user-defined implicit conversions:
            // SPEC VIOLATION: 1) To string type (applicable in normal form): public static implicit operator string (Conv? C2)
            // SPEC VIOLATION: 2) To int? type (applicable in half-lifted form): public static implicit operator int? (Conv C)

            // SPEC VIOLATION: This occurs because the native compiler compares the applicable conversions to find one with the least amount
            // SPEC VIOLATION: of lifting, ignoring whether the return types are the same or not.
            // SPEC VIOLATION: We do the same to maintain compatibility with the native compiler.

            // (a) Compute the set of types D from which user-defined conversion operators should be considered by considering only the source type.
            var d = ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol ConstrainedToTypeOpt)>.GetInstance();
            AddTypesParticipatingInUserDefinedConversion(d, source, null, useSiteInfo: ref useSiteInfo);

            // (b) Instead of computing applicable user defined implicit conversions U from the source type to a specific target type,
            //     we compute these from the source type to ANY target type. We will filter out those that are valid switch governing
            //     types later.
            var ubuild = ArrayBuilder<UserDefinedConversionAnalysis>.GetInstance();
            ComputeApplicableUserDefinedImplicitConversionSet(sourceExpression: null, source, target: null, d: d, u: ubuild, useSiteInfo: ref useSiteInfo, allowAnyTarget: true);
            d.Free();
            ImmutableArray<UserDefinedConversionAnalysis> u = ubuild.ToImmutableAndFree();

            // (c) Find that conversion with the least amount of lifting
            int? best = MostSpecificConversionOperator(conv => conv.ToType.IsValidV6SwitchGoverningType(isTargetTypeOfUserDefinedOp: true), u);
            if (best != null)
            {
                return UserDefinedConversionResult.Valid(u, best.Value);
            }

            return UserDefinedConversionResult.NoApplicableOperators(u);
        }
    }
}
