// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ModifierUtils
    {
        internal static DeclarationModifiers MakeAndCheckNonTypeMemberModifiers(
            bool isOrdinaryMethod,
            bool isForInterfaceMember,
            SyntaxTokenList modifiers,
            DeclarationModifiers defaultAccess,
            DeclarationModifiers allowedModifiers,
            Location errorLocation,
            BindingDiagnosticBag diagnostics,
            out bool modifierErrors)
        {
            var result = modifiers.ToDeclarationModifiers(diagnostics.DiagnosticBag ?? new DiagnosticBag(), isOrdinaryMethod: isOrdinaryMethod);
            result = CheckModifiers(isForTypeDeclaration: false, isForInterfaceMember, result, allowedModifiers, errorLocation, diagnostics, out modifierErrors);

            if ((result & DeclarationModifiers.AccessibilityMask) == 0)
                result |= defaultAccess;

            return result;
        }

        internal static DeclarationModifiers CheckModifiers(
            bool isForTypeDeclaration,
            bool isForInterfaceMember,
            DeclarationModifiers modifiers,
            DeclarationModifiers allowedModifiers,
            Location errorLocation,
            BindingDiagnosticBag diagnostics,
            out bool modifierErrors)
        {
            Debug.Assert(!isForTypeDeclaration || !isForInterfaceMember);

            modifierErrors = false;
            DeclarationModifiers reportStaticNotVirtualForModifiers = DeclarationModifiers.None;

            if (isForTypeDeclaration)
            {
                Debug.Assert((allowedModifiers & (DeclarationModifiers.Override | DeclarationModifiers.Virtual)) == 0);
            }
            else if ((modifiers & allowedModifiers & DeclarationModifiers.Static) != 0)
            {
                if (isForInterfaceMember)
                {
                    reportStaticNotVirtualForModifiers = allowedModifiers & DeclarationModifiers.Override;
                }
                else
                {
                    reportStaticNotVirtualForModifiers = allowedModifiers & (DeclarationModifiers.Abstract | DeclarationModifiers.Override | DeclarationModifiers.Virtual);
                }

                allowedModifiers &= ~reportStaticNotVirtualForModifiers;
            }

            DeclarationModifiers errorModifiers = modifiers & ~allowedModifiers;
            DeclarationModifiers result = modifiers & allowedModifiers;

            while (errorModifiers != DeclarationModifiers.None)
            {
                DeclarationModifiers oneError = errorModifiers & ~(errorModifiers - 1);
                Debug.Assert(oneError != DeclarationModifiers.None);
                errorModifiers &= ~oneError;

                switch (oneError)
                {
                    case DeclarationModifiers.Partial:
                        break;

                    case DeclarationModifiers.Abstract:
                    case DeclarationModifiers.Override:
                    case DeclarationModifiers.Virtual:
                        if ((reportStaticNotVirtualForModifiers & oneError) == 0)
                        {
                            goto default;
                        }

                        diagnostics.Add(ErrorCode.ERR_StaticNotVirtual, errorLocation, ModifierUtils.ConvertSingleModifierToSyntaxText(oneError));
                        break;

                    default:
                        diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, ConvertSingleModifierToSyntaxText(oneError));
                        break;
                }

                modifierErrors = true;
            }

            return result;
        }

        internal static void ReportDefaultInterfaceImplementationModifiers(
            DeclarationModifiers modifiers,
            DeclarationModifiers defaultInterfaceImplementationModifiers,
            Location errorLocation,
            BindingDiagnosticBag diagnostics)
        {
            if ((modifiers & defaultInterfaceImplementationModifiers) != 0)
            {
                if ((modifiers & defaultInterfaceImplementationModifiers & DeclarationModifiers.Static) != 0 &&
                    (modifiers & defaultInterfaceImplementationModifiers & (DeclarationModifiers.Sealed | DeclarationModifiers.Abstract | DeclarationModifiers.Virtual)) != 0)
                {
                    var reportModifiers = DeclarationModifiers.Sealed | DeclarationModifiers.Abstract | DeclarationModifiers.Virtual;
                    if ((modifiers & defaultInterfaceImplementationModifiers & DeclarationModifiers.Sealed) != 0 &&
                        (modifiers & defaultInterfaceImplementationModifiers & (DeclarationModifiers.Abstract | DeclarationModifiers.Virtual)) != 0)
                    {
                        diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, ConvertSingleModifierToSyntaxText(DeclarationModifiers.Sealed));
                        reportModifiers &= ~DeclarationModifiers.Sealed;
                    }

                    return; // below we will either ask for an earlier version of the language, or will not report anything
                }
            }
        }

        internal static DeclarationModifiers AdjustModifiersForAnInterfaceMember(DeclarationModifiers mods, bool hasBody, bool isExplicitInterfaceImplementation)
        {
            if (isExplicitInterfaceImplementation)
            {
                if ((mods & DeclarationModifiers.Abstract) != 0)
                {
                    mods |= DeclarationModifiers.Sealed;
                }
            }
            else if ((mods & DeclarationModifiers.Static) != 0)
            {
                mods &= ~DeclarationModifiers.Sealed;
            }
            else if ((mods & (DeclarationModifiers.Private | DeclarationModifiers.Partial | DeclarationModifiers.Virtual | DeclarationModifiers.Abstract)) == 0)
            {
                Debug.Assert(!isExplicitInterfaceImplementation);

                if (hasBody || (mods & (DeclarationModifiers.Extern | DeclarationModifiers.Sealed)) != 0)
                {
                    if ((mods & DeclarationModifiers.Sealed) == 0)
                    {
                        mods |= DeclarationModifiers.Virtual;
                    }
                    else
                    {
                        mods &= ~DeclarationModifiers.Sealed;
                    }
                }
                else
                {
                    mods |= DeclarationModifiers.Abstract;
                }
            }

            if ((mods & DeclarationModifiers.AccessibilityMask) == 0)
            {
                if ((mods & DeclarationModifiers.Partial) == 0 && !isExplicitInterfaceImplementation)
                {
                    mods |= DeclarationModifiers.Public;
                }
                else
                {
                    mods |= DeclarationModifiers.Private;
                }
            }

            return mods;
        }

        internal static string ConvertSingleModifierToSyntaxText(DeclarationModifiers modifier)
        {
            switch (modifier)
            {
                case DeclarationModifiers.Abstract:
                    return SyntaxFacts.GetText(SyntaxKind.AbstractKeyword);
                case DeclarationModifiers.Sealed:
                    return SyntaxFacts.GetText(SyntaxKind.SealedKeyword);
                case DeclarationModifiers.Static:
                    return SyntaxFacts.GetText(SyntaxKind.StaticKeyword);
                case DeclarationModifiers.New:
                    return SyntaxFacts.GetText(SyntaxKind.NewKeyword);
                case DeclarationModifiers.Public:
                    return SyntaxFacts.GetText(SyntaxKind.PublicKeyword);
                case DeclarationModifiers.Protected:
                    return SyntaxFacts.GetText(SyntaxKind.ProtectedKeyword);
                case DeclarationModifiers.Internal:
                    return SyntaxFacts.GetText(SyntaxKind.InternalKeyword);
                case DeclarationModifiers.ProtectedInternal:
                    return SyntaxFacts.GetText(SyntaxKind.ProtectedKeyword) + " " + SyntaxFacts.GetText(SyntaxKind.InternalKeyword);
                case DeclarationModifiers.Private:
                    return SyntaxFacts.GetText(SyntaxKind.PrivateKeyword);
                case DeclarationModifiers.PrivateProtected:
                    return SyntaxFacts.GetText(SyntaxKind.PrivateKeyword) + " " + SyntaxFacts.GetText(SyntaxKind.ProtectedKeyword);
                case DeclarationModifiers.ReadOnly:
                    return SyntaxFacts.GetText(SyntaxKind.ReadOnlyKeyword);
                case DeclarationModifiers.Const:
                    return SyntaxFacts.GetText(SyntaxKind.ConstKeyword);
                case DeclarationModifiers.Volatile:
                    return SyntaxFacts.GetText(SyntaxKind.VolatileKeyword);
                case DeclarationModifiers.Extern:
                    return SyntaxFacts.GetText(SyntaxKind.ExternKeyword);
                case DeclarationModifiers.Partial:
                    return SyntaxFacts.GetText(SyntaxKind.PartialKeyword);
                case DeclarationModifiers.Unsafe:
                    return SyntaxFacts.GetText(SyntaxKind.UnsafeKeyword);
                case DeclarationModifiers.Fixed:
                    return SyntaxFacts.GetText(SyntaxKind.FixedKeyword);
                case DeclarationModifiers.Virtual:
                    return SyntaxFacts.GetText(SyntaxKind.VirtualKeyword);
                case DeclarationModifiers.Override:
                    return SyntaxFacts.GetText(SyntaxKind.OverrideKeyword);
                case DeclarationModifiers.Async:
                    return SyntaxFacts.GetText(SyntaxKind.AsyncKeyword);
                case DeclarationModifiers.Ref:
                    return SyntaxFacts.GetText(SyntaxKind.RefKeyword);
                case DeclarationModifiers.Required:
                    return SyntaxFacts.GetText(SyntaxKind.RequiredKeyword);
                case DeclarationModifiers.Scoped:
                    return SyntaxFacts.GetText(SyntaxKind.ScopedKeyword);
                case DeclarationModifiers.File:
                    return SyntaxFacts.GetText(SyntaxKind.FileKeyword);
                default:
                    throw ExceptionUtilities.UnexpectedValue(modifier);
            }
        }

        private static DeclarationModifiers ToDeclarationModifier(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.AbstractKeyword:
                    return DeclarationModifiers.Abstract;
                case SyntaxKind.AsyncKeyword:
                    return DeclarationModifiers.Async;
                case SyntaxKind.SealedKeyword:
                    return DeclarationModifiers.Sealed;
                case SyntaxKind.StaticKeyword:
                    return DeclarationModifiers.Static;
                case SyntaxKind.NewKeyword:
                    return DeclarationModifiers.New;
                case SyntaxKind.PublicKeyword:
                    return DeclarationModifiers.Public;
                case SyntaxKind.ProtectedKeyword:
                    return DeclarationModifiers.Protected;
                case SyntaxKind.InternalKeyword:
                    return DeclarationModifiers.Internal;
                case SyntaxKind.PrivateKeyword:
                    return DeclarationModifiers.Private;
                case SyntaxKind.ExternKeyword:
                    return DeclarationModifiers.Extern;
                case SyntaxKind.ReadOnlyKeyword:
                    return DeclarationModifiers.ReadOnly;
                case SyntaxKind.PartialKeyword:
                    return DeclarationModifiers.Partial;
                case SyntaxKind.UnsafeKeyword:
                    return DeclarationModifiers.Unsafe;
                case SyntaxKind.VirtualKeyword:
                    return DeclarationModifiers.Virtual;
                case SyntaxKind.OverrideKeyword:
                    return DeclarationModifiers.Override;
                case SyntaxKind.ConstKeyword:
                    return DeclarationModifiers.Const;
                case SyntaxKind.FixedKeyword:
                    return DeclarationModifiers.Fixed;
                case SyntaxKind.VolatileKeyword:
                    return DeclarationModifiers.Volatile;
                case SyntaxKind.RefKeyword:
                    return DeclarationModifiers.Ref;
                case SyntaxKind.RequiredKeyword:
                    return DeclarationModifiers.Required;
                case SyntaxKind.ScopedKeyword:
                    return DeclarationModifiers.Scoped;
                case SyntaxKind.FileKeyword:
                    return DeclarationModifiers.File;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        public static DeclarationModifiers ToDeclarationModifiers(this SyntaxTokenList modifiers, 
            DiagnosticBag diagnostics, bool isOrdinaryMethod = false)
        {
            var result = DeclarationModifiers.None;
            bool seenNoDuplicates = true;

            for (int i = 0; i < modifiers.Count; i++)
            {
                SyntaxToken modifier = modifiers[i];
                DeclarationModifiers one = ToDeclarationModifier(modifier.ContextualKind());

                ReportDuplicateModifiers(
                    modifier, one, result,
                    ref seenNoDuplicates,
                    diagnostics);

                if (one == DeclarationModifiers.Partial)
                {
                    // `partial` must always be the last modifier according to the language.  However, there was a bug
                    // where we allowed `partial async` at the end of modifiers on methods. We keep this behavior for
                    // backcompat.
                    var isLast = i == modifiers.Count - 1;
                    var isPartialAsyncMethod = isOrdinaryMethod && i == modifiers.Count - 2 && modifiers[i + 1].ContextualKind() is SyntaxKind.AsyncKeyword;
                    if (!isLast && !isPartialAsyncMethod)
                    {
                        //Dev69
                    }
                }

                result |= one;
            }

            switch (result & DeclarationModifiers.AccessibilityMask)
            {
                case DeclarationModifiers.Protected | DeclarationModifiers.Internal:
                    // the two keywords "protected" and "internal" together are treated as one modifier.
                    result &= ~DeclarationModifiers.AccessibilityMask;
                    result |= DeclarationModifiers.ProtectedInternal;
                    break;

                case DeclarationModifiers.Private | DeclarationModifiers.Protected:
                    // the two keywords "private" and "protected" together are treated as one modifier.
                    result &= ~DeclarationModifiers.AccessibilityMask;
                    result |= DeclarationModifiers.PrivateProtected;
                    break;
            }

            return result;
        }

        private static void ReportDuplicateModifiers(
            SyntaxToken modifierToken,
            DeclarationModifiers modifierKind,
            DeclarationModifiers allModifiers,
            ref bool seenNoDuplicates,
            DiagnosticBag diagnostics)
        {
            if ((allModifiers & modifierKind) != 0)
            {
                if (seenNoDuplicates)
                {
                    diagnostics.Add(
                        ErrorCode.ERR_DuplicateModifier,
                        modifierToken.GetLocation(),
                        SyntaxFacts.GetText(modifierToken.Kind()));
                    seenNoDuplicates = false;
                }
            }
        }

        internal static bool CheckAccessibility(DeclarationModifiers modifiers, Symbol symbol, bool isExplicitInterfaceImplementation, BindingDiagnosticBag diagnostics, Location errorLocation)
        {
            if (!IsValidAccessibility(modifiers))
            {
                // error CS0107: More than one protection modifier
                diagnostics.Add(ErrorCode.ERR_BadMemberProtection, errorLocation);
                return true;
            }

            if ((modifiers & DeclarationModifiers.Required) != 0)
            {
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(futureDestination: diagnostics, assemblyBeingBuilt: symbol.ContainingAssembly);
                bool reportedError = false;

                switch (symbol)
                {
                    case FieldSymbol when !symbol.IsAsRestrictive(symbol.ContainingType, ref useSiteInfo):
                    case PropertySymbol { SetMethod: { } method } when !method.IsAsRestrictive(symbol.ContainingType, ref useSiteInfo):
                        // Required member '{0}' cannot be less visible or have a setter less visible than the containing type '{1}'.
                        diagnostics.Add(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, errorLocation, symbol, symbol.ContainingType);
                        reportedError = true;
                        break;
                    case PropertySymbol { SetMethod: null }:
                    case FieldSymbol when (modifiers & DeclarationModifiers.ReadOnly) != 0:
                        // Required member '{0}' must be settable.
                        diagnostics.Add(ErrorCode.ERR_RequiredMemberMustBeSettable, errorLocation, symbol);
                        reportedError = true;
                        break;
                }

                diagnostics.Add(errorLocation, useSiteInfo);
                return reportedError;
            }

            return false;
        }

        // Returns declared accessibility.
        // In a case of bogus accessibility (i.e. "public private"), defaults to public.
        internal static Accessibility EffectiveAccessibility(DeclarationModifiers modifiers)
        {
            switch (modifiers & DeclarationModifiers.AccessibilityMask)
            {
                case DeclarationModifiers.None:
                    return Accessibility.NotApplicable; // for explicit interface implementation
                case DeclarationModifiers.Private:
                    return Accessibility.Private;
                case DeclarationModifiers.Protected:
                    return Accessibility.Protected;
                case DeclarationModifiers.Internal:
                    return Accessibility.Internal;
                case DeclarationModifiers.Public:
                    return Accessibility.Public;
                case DeclarationModifiers.ProtectedInternal:
                    return Accessibility.ProtectedOrInternal;
                case DeclarationModifiers.PrivateProtected:
                    return Accessibility.ProtectedAndInternal;
                default:
                    // This happens when you have a mix of accessibilities.
                    //
                    // i.e.: public private void Goo()
                    return Accessibility.Public;
            }
        }

        internal static bool IsValidAccessibility(DeclarationModifiers modifiers)
        {
            switch (modifiers & DeclarationModifiers.AccessibilityMask)
            {
                case DeclarationModifiers.None:
                case DeclarationModifiers.Private:
                case DeclarationModifiers.Protected:
                case DeclarationModifiers.Internal:
                case DeclarationModifiers.Public:
                case DeclarationModifiers.ProtectedInternal:
                case DeclarationModifiers.PrivateProtected:
                    return true;

                default:
                    // This happens when you have a mix of accessibilities.
                    //
                    // i.e.: public private void Goo()
                    return false;
            }
        }
    }
}
