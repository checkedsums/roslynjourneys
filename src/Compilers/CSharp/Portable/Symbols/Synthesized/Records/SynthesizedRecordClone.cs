﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// If a virtual "clone" method is present in the base record, the synthesized "clone" method overrides it
    /// and the return type of the method is the current containing type if the "covariant returns" feature is
    /// supported and the override return type otherwise. An error is produced if the base record clone method
    /// is sealed. If a virtual "clone" method is not present in the base record, the return type of the clone
    /// method is the containing type and the method is virtual, unless the record is sealed or abstract.
    /// If the containing record is abstract, the synthesized clone method is also abstract.
    /// If the "clone" method is not abstract, it returns the result of a call to a copy constructor.
    /// </summary>
    internal sealed class SynthesizedRecordClone : SynthesizedRecordOrdinaryMethod
    {
        public SynthesizedRecordClone(
            SourceMemberContainerTypeSymbol containingType,
            int memberOffset)
            : base(containingType, WellKnownMemberNames.CloneMethodName, memberOffset, MakeDeclarationModifiers(containingType))
        {
            Debug.Assert(!containingType.IsRecordStruct);
        }

        private static DeclarationModifiers MakeDeclarationModifiers(SourceMemberContainerTypeSymbol containingType)
        {
            DeclarationModifiers result = DeclarationModifiers.Public;

            if (VirtualCloneInBase(containingType) is not null)
            {
                result |= DeclarationModifiers.Override;
            }
            else
            {
                result |= containingType.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual;
            }

            if (containingType.IsAbstract)
            {
                result &= ~DeclarationModifiers.Virtual;
                result |= DeclarationModifiers.Abstract;
            }

#if DEBUG
            Debug.Assert(modifiersAreValid(result));
#endif 
            return result;

#if DEBUG
            static bool modifiersAreValid(DeclarationModifiers modifiers)
            {
                if ((modifiers & DeclarationModifiers.AccessibilityMask) != DeclarationModifiers.Public)
                {
                    return false;
                }

                modifiers &= ~DeclarationModifiers.AccessibilityMask;

                return modifiers switch
                {
                    DeclarationModifiers.None => true,
                    DeclarationModifiers.Abstract => true,
                    DeclarationModifiers.Override => true,
                    DeclarationModifiers.Abstract | DeclarationModifiers.Override => true,
                    DeclarationModifiers.Virtual => true,
                    _ => false,
                };
            }
#endif 
        }

        private static MethodSymbol? VirtualCloneInBase(NamedTypeSymbol containingType)
        {
            NamedTypeSymbol baseType = containingType.BaseTypeNoUseSiteDiagnostics;

            if (!baseType.IsObjectType())
            {
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded; // This is reported when we bind bases
                return FindValidCloneMethod(baseType, ref discardedUseSiteInfo);
            }

            return null;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            return (ReturnType: TypeWithAnnotations.Create(isNullableEnabled: true, ContainingType),
                    Parameters: ImmutableArray<ParameterSymbol>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 0;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!IsAbstract);

            var f = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                if (ReturnType.IsErrorType())
                {
                    f.CloseMethod(f.ThrowNull());
                    return;
                }

                var members = ContainingType.InstanceConstructors;
                foreach (var member in members)
                {
                    var ctor = member;
                    if (ctor.ParameterCount == 1 && ctor.Parameters[0].RefKind == RefKind.None &&
                        ctor.Parameters[0].Type.Equals(ContainingType, TypeCompareKind.AllIgnoreOptions))
                    {
                        f.CloseMethod(f.Return(f.New(ctor, f.This())));
                        return;
                    }
                }

                throw ExceptionUtilities.Unreachable();
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                f.CloseMethod(f.ThrowNull());
            }
        }

        internal static MethodSymbol? FindValidCloneMethod(TypeSymbol containingType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (containingType.IsObjectType() || containingType is not NamedTypeSymbol containingNamedType)
            {
                return null;
            }

            // If this symbol is from metadata, getting all members can cause us to realize a lot of structures that we otherwise
            // don't have to. Optimize for the common case here of there not being a method named <Clone>$. If there is a method
            // with that name, it's most likely the one we're interested in, and we can't get around loading everything to find it.
            if (!containingNamedType.HasPossibleWellKnownCloneMethod())
            {
                return null;
            }

            MethodSymbol? candidate = null;

            foreach (var member in containingType.GetMembers(WellKnownMemberNames.CloneMethodName))
            {
                if (member is MethodSymbol
                    {
                        DeclaredAccessibility: Accessibility.Public,
                        IsStatic: false,
                        ParameterCount: 0,
                        Arity: 0
                    } method)
                {
                    if (candidate is not null)
                    {
                        // An ambiguity case, can come from metadata, treat as an error for simplicity.
                        return null;
                    }

                    candidate = method;
                }
            }

            if (candidate is null ||
                !(containingType.IsSealed || candidate.IsOverride || candidate.IsVirtual || candidate.IsAbstract) ||
                !containingType.IsEqualToOrDerivedFrom(
                    candidate.ReturnType,
                    TypeCompareKind.AllIgnoreOptions,
                    ref useSiteInfo))
            {
                return null;
            }

            return candidate;
        }
    }
}
