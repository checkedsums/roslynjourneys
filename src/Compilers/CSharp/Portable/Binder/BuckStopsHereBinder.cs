// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that knows no symbols and will not delegate further.
    /// </summary>
    internal class BuckStopsHereBinder : Binder
    {
        internal BuckStopsHereBinder(CSharpCompilation compilation, FileIdentifier? associatedFileIdentifier)
            : base(compilation)
        {
            this.AssociatedFileIdentifier = associatedFileIdentifier;
        }

        /// <summary>
        /// * In non-speculative scenarios, the identifier for the file being bound.
        /// * In speculative scenarios, the identifier for the file from the original compilation used as the speculation context.
        /// * In EE scenarios, the identifier for the file from the original compilation used as the evaluation context.
        /// 
        /// This is <see langword="null"/> in some scenarios, such as the binder used for <see cref="CSharpCompilation.Conversions" />
        /// or the binder used to bind usings in <see cref="CSharpCompilation.UsingsFromOptionsAndDiagnostics"/>.
        /// </summary>
        internal readonly FileIdentifier? AssociatedFileIdentifier;

        internal override ImportChain? ImportChain => null;

        /// <summary>
        /// Get <see cref="QuickAttributeChecker"/> that can be used to quickly
        /// check for certain attribute applications in context of this binder.
        /// </summary>
        internal override QuickAttributeChecker QuickAttributeChecker => QuickAttributeChecker.Predefined;

        protected override SourceLocalSymbol? LookupLocal(SyntaxToken nameToken) => null;

        protected override LocalFunctionSymbol? LookupLocalFunction(SyntaxToken nameToken) => null;

        protected override bool InExecutableBinder => false;
        protected override SyntaxNode? EnclosingNameofArgument => null;
        internal override bool IsInsideNameof => false;

        internal override bool IsAccessibleHelper(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, ConsList<TypeSymbol> basesBeingResolved)
        {
            failedThroughTypeCheck = false;
            return IsSymbolAccessibleConditional(symbol, Compilation.Assembly, ref useSiteInfo);
        }

        internal override ConstantFieldsInProgress ConstantFieldsInProgress => ConstantFieldsInProgress.Empty;

        internal override ConsList<FieldSymbol> FieldsBeingBound => ConsList<FieldSymbol>.Empty;

        internal override LocalSymbol? LocalInProgress => null;

        protected override bool IsUnboundTypeAllowed(GenericNameSyntax syntax) => false;

        internal override bool IsInMethodBody => false;

        internal override bool IsDirectlyInIterator => false;

        internal override bool IsIndirectlyInIterator => false;

        internal override GeneratedLabelSymbol? BreakLabel => null;

        internal override GeneratedLabelSymbol? ContinueLabel => null;

        internal override BoundExpression? ConditionalReceiverExpression => null;

        // This should only be called in the context of syntactically incorrect programs.  In other
        // contexts statements are surrounded by some enclosing method or lambda.
        internal override TypeWithAnnotations GetIteratorElementType() =>
            // There's supposed to be an enclosing method or lambda.
            throw ExceptionUtilities.Unreachable();

        internal override Symbol? ContainingMemberOrLambda => null;

        internal override Binder? GetBinder(SyntaxNode node) => null;

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator) => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator) => throw ExceptionUtilities.Unreachable();

        internal override BoundStatement BindSwitchStatementCore(SwitchStatementSyntax node, Binder originalBinder, BindingDiagnosticBag diagnostics) =>
            // There's supposed to be a SwitchBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundExpression BindSwitchExpressionCore(SwitchExpressionSyntax node, Binder originalBinder, BindingDiagnosticBag diagnostics) =>
            // There's supposed to be a SwitchExpressionBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override void BindPatternSwitchLabelForInference(CasePatternSwitchLabelSyntax node, BindingDiagnosticBag diagnostics) =>
            // There's supposed to be a SwitchBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundSwitchExpressionArm BindSwitchExpressionArm(SwitchExpressionArmSyntax node, TypeSymbol switchGoverningType, BindingDiagnosticBag diagnostics) =>
            // There's supposed to be an overrider of this method (e.g. SwitchExpressionArmBinder) for the arm in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundForStatement BindForParts(BindingDiagnosticBag diagnostics, Binder originalBinder) =>
            // There's supposed to be a ForLoopBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundStatement BindForEachParts(BindingDiagnosticBag diagnostics, Binder originalBinder) =>
            // There's supposed to be a ForEachLoopBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundStatement BindForEachDeconstruction(BindingDiagnosticBag diagnostics, Binder originalBinder) =>
            // There's supposed to be a ForEachLoopBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundWhileStatement BindWhileParts(BindingDiagnosticBag diagnostics, Binder originalBinder) =>
            // There's supposed to be a WhileBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundDoStatement BindDoParts(BindingDiagnosticBag diagnostics, Binder originalBinder) =>
            // There's supposed to be a WhileBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundStatement BindUsingStatementParts(BindingDiagnosticBag diagnostics, Binder originalBinder) =>
            // There's supposed to be a UsingStatementBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override BoundStatement BindLockStatementParts(BindingDiagnosticBag diagnostics, Binder originalBinder) =>
            // There's supposed to be a LockBinder (or other overrider of this method) in the chain.
            throw ExceptionUtilities.Unreachable();

        internal override ImmutableHashSet<Symbol> LockedOrDisposedVariables => ImmutableHashSet.Create<Symbol>();
    }
}
