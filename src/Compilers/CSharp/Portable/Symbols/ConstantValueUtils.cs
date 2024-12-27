// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class EvaluatedConstant(ConstantValue value, ReadOnlyBindingDiagnostic<AssemblySymbol> diagnostics)
    {
        public readonly ConstantValue Value = value;
        public readonly ReadOnlyBindingDiagnostic<AssemblySymbol> Diagnostics = diagnostics.NullToEmpty();
    }

    internal static class ConstantValueUtils
    {
        public static ConstantValue EvaluateFieldConstant(
            SourceFieldSymbol symbol,
            EqualsValueClauseSyntax equalsValueNode,
            HashSet<SourceFieldSymbolWithSyntaxReference> dependencies,
            bool earlyDecodingWellKnownAttributes,
            BindingDiagnosticBag diagnostics)
        {
            var compilation = symbol.DeclaringCompilation;
            var binderFactory = compilation.GetBinderFactory(equalsValueNode.SyntaxTree);
            var binder = binderFactory.GetBinder(equalsValueNode);

            binder = new WithPrimaryConstructorParametersBinder(symbol.ContainingType, binder);

            if (earlyDecodingWellKnownAttributes)
            {
                binder = new EarlyWellKnownAttributeBinder(binder);
            }
            var inProgressBinder = new ConstantFieldsInProgressBinder(new ConstantFieldsInProgress(symbol, dependencies), binder);
            BoundFieldEqualsValue boundValue = BindFieldOrEnumInitializer(inProgressBinder, symbol, equalsValueNode, diagnostics);

            var value = GetAndValidateConstantValue(boundValue.Value, symbol, symbol.Type, equalsValueNode.Value, diagnostics);
            Debug.Assert(value != null);

            return value;
        }

        private static BoundFieldEqualsValue BindFieldOrEnumInitializer(
            Binder binder,
            FieldSymbol fieldSymbol,
            EqualsValueClauseSyntax initializer,
            BindingDiagnosticBag diagnostics)
        {
            var enumConstant = fieldSymbol as SourceEnumConstantSymbol;
            Binder collisionDetector = new LocalScopeBinder(binder);
            collisionDetector = new ExecutableCodeBinder(initializer, fieldSymbol, collisionDetector);
            BoundFieldEqualsValue result;

            if (enumConstant is not null)
            {
                result = collisionDetector.BindEnumConstantInitializer(enumConstant, initializer, diagnostics);
            }
            else
            {
                result = collisionDetector.BindFieldInitializer(fieldSymbol, initializer, diagnostics);
            }

            return result;
        }

        internal static ConstantValue GetAndValidateConstantValue(
            BoundExpression boundValue,
            Symbol thisSymbol,
            TypeSymbol typeSymbol,
            SyntaxNode initValueNode,
            BindingDiagnosticBag diagnostics)
        {
            var value = ConstantValue.Bad;
            CheckConstantValue(boundValue, diagnostics);
            if (!boundValue.HasAnyErrors)
            {
                if (typeSymbol.TypeKind == TypeKind.TypeParameter)
                {
                    diagnostics.Add(ErrorCode.ERR_InvalidConstantDeclarationType, initValueNode.Location, thisSymbol, typeSymbol);
                }
                else
                {
                    bool hasDynamicConversion = false;
                    var unconvertedBoundValue = boundValue;
                    while (unconvertedBoundValue.Kind == BoundKind.Conversion)
                    {
                        var conversion = (BoundConversion)unconvertedBoundValue;
                        hasDynamicConversion = hasDynamicConversion || conversion.ConversionKind.IsDynamic();
                        unconvertedBoundValue = conversion.Operand;
                    }

                    // If we have already computed the unconverted constant value, then this call is cheap
                    // because BoundConversions store their constant values (i.e. not recomputing anything).
                    var constantValue = boundValue.ConstantValueOpt;

                    var unconvertedConstantValue = unconvertedBoundValue.ConstantValueOpt;
                    if (unconvertedConstantValue != null &&
                        !unconvertedConstantValue.IsNull &&
                        typeSymbol.IsReferenceType &&
                        typeSymbol.SpecialType != SpecialType.System_String)
                    {
                        // Suppose we are in this case:
                        //
                        // const object x = "some_string"
                        //
                        // A constant of type object can only be initialized to
                        // null; it may not contain an implicit reference conversion
                        // from string.
                        //
                        // Give a special error for that case.
                        diagnostics.Add(ErrorCode.ERR_NotNullConstRefField, initValueNode.Location, thisSymbol, typeSymbol);

                        // If we get here, then the constantValue will likely be null.
                        // However, it seems reasonable to assume that the programmer will correct the error not
                        // by changing the value to "null", but by updating the type of the constant.  Consequently,
                        // we retain the unconverted constant value so that it can propagate through the rest of
                        // constant folding.
                        constantValue = constantValue ?? unconvertedConstantValue;
                    }

                    if (constantValue != null && !hasDynamicConversion)
                    {
                        value = constantValue;
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_NotConstantExpression, initValueNode.Location, thisSymbol);
                    }
                }
            }

            return value;
        }

        private sealed class CheckConstantInterpolatedStringValidity() : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
            {
                return null;
            }
        }

        internal static void CheckConstantValue(BoundExpression expression, BindingDiagnosticBag _)
        {
            if (expression.Type is not null && expression.Type.IsStringType())
            {
                var visitor = new CheckConstantInterpolatedStringValidity();
                visitor.Visit(expression);
            }
        }
    }
}
