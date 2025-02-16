﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class UsingStatementBinder : LockOrUsingBinder
    {
        private readonly UsingStatementSyntax _syntax;

        public UsingStatementBinder(Binder enclosing, UsingStatementSyntax syntax)
            : base(enclosing)
        {
            _syntax = syntax;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            ExpressionSyntax expressionSyntax = TargetExpressionSyntax;
            VariableDeclarationSyntax declarationSyntax = _syntax.Declaration;

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            if (expressionSyntax != null)
            {
                var locals = ArrayBuilder<LocalSymbol>.GetInstance();
                ExpressionVariableFinder.FindExpressionVariables(this, locals, expressionSyntax);
                return locals.ToImmutableAndFree();
            }
            else
            {
                var locals = ArrayBuilder<LocalSymbol>.GetInstance(declarationSyntax.Variables.Count);

                // gather expression-declared variables from invalid array dimensions. eg. using(int[x is var y] z = new int[0])
                declarationSyntax.Type.VisitRankSpecifiers((rankSpecifier, args) =>
                {
                    foreach (var size in rankSpecifier.Sizes)
                    {
                        if (size.Kind() != SyntaxKind.OmittedArraySizeExpression)
                        {
                            ExpressionVariableFinder.FindExpressionVariables(args.binder, args.locals, size);
                        }
                    }
                }, (binder: this, locals: locals));

                foreach (VariableDeclaratorSyntax declarator in declarationSyntax.Variables)
                {
                    locals.Add(MakeLocal(declarationSyntax, declarator, LocalDeclarationKind.UsingVariable, allowScoped: true));

                    // also gather expression-declared variables from the bracketed argument lists and the initializers
                    ExpressionVariableFinder.FindExpressionVariables(this, locals, declarator);
                }

                return locals.ToImmutableAndFree();
            }
        }

        protected override ExpressionSyntax TargetExpressionSyntax
        {
            get
            {
                return _syntax.Expression;
            }
        }

        internal override BoundStatement BindUsingStatementParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            ExpressionSyntax expressionSyntax = TargetExpressionSyntax;
            VariableDeclarationSyntax declarationSyntax = _syntax.Declaration;
            bool hasAwait = _syntax.AwaitKeyword.Kind() != default;

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            var boundUsingStatement = BindUsingStatementOrDeclarationFromParts((CSharpSyntaxNode)expressionSyntax ?? declarationSyntax, _syntax.UsingKeyword, _syntax.AwaitKeyword, originalBinder, this, diagnostics);
            Debug.Assert(boundUsingStatement is BoundUsingStatement);
            return boundUsingStatement;
        }

#nullable enable
        internal static BoundStatement BindUsingStatementOrDeclarationFromParts(SyntaxNode syntax, SyntaxToken usingKeyword, SyntaxToken awaitKeyword, Binder originalBinder, UsingStatementBinder? usingBinder, BindingDiagnosticBag diagnostics)
        {
            bool isUsingDeclaration = syntax.Kind() == SyntaxKind.LocalDeclarationStatement;
            bool isExpression = !isUsingDeclaration && syntax.Kind() != SyntaxKind.VariableDeclaration;
            bool hasAwait = awaitKeyword != default;

            Debug.Assert(isUsingDeclaration || usingBinder != null);

            bool hasErrors = false;
            ImmutableArray<BoundLocalDeclaration> declarationsOpt = default;
            BoundMultipleLocalDeclarations? multipleDeclarations = null;
            BoundExpression? expression = null;
            TypeSymbol? declarationType = null;
            MethodArgumentInfo? patternDisposeInfo;
            TypeSymbol? awaitableType;

            if (isExpression)
            {
                expression = usingBinder!.BindTargetExpression(diagnostics, originalBinder);
                hasErrors |= !bindDisposable(fromExpression: true, out patternDisposeInfo, out awaitableType);
            }
            else
            {
                VariableDeclarationSyntax declarationSyntax = isUsingDeclaration ? ((LocalDeclarationStatementSyntax)syntax).Declaration : (VariableDeclarationSyntax)syntax;
                originalBinder.BindForOrUsingOrFixedDeclarations(declarationSyntax, LocalDeclarationKind.UsingVariable, diagnostics, out declarationsOpt);

                Debug.Assert(!declarationsOpt.IsEmpty && declarationsOpt[0].DeclaredTypeOpt != null);
                multipleDeclarations = new BoundMultipleLocalDeclarations(declarationSyntax, declarationsOpt);
                declarationType = declarationsOpt[0].DeclaredTypeOpt!.Type;

                if (declarationType.IsDynamic())
                {
                    patternDisposeInfo = null;
                    awaitableType = null;
                }
                else
                {
                    hasErrors |= !bindDisposable(fromExpression: false, out patternDisposeInfo, out awaitableType);
                }
            }

            BoundAwaitableInfo? await = null;
            if (hasAwait)
            {
                // even if we don't have a proper value to await, we'll still report bad usages of `await`
                originalBinder.ReportBadAwaitDiagnostics(awaitKeyword, diagnostics, ref hasErrors);

                if (awaitableType is null)
                {
                    await = new BoundAwaitableInfo(syntax, awaitableInstancePlaceholder: null, isDynamic: true, getAwaiter: null, isCompleted: null, getResult: null) { WasCompilerGenerated = true };
                }
                else
                {
                    hasErrors |= ReportUseSite(awaitableType, diagnostics, awaitKeyword);
                    var placeholder = new BoundAwaitableValuePlaceholder(syntax, awaitableType).MakeCompilerGenerated();
                    await = originalBinder.BindAwaitInfo(placeholder, syntax, diagnostics, ref hasErrors);
                }
            }

            // This is not awesome, but its factored. 
            // In the future it might be better to have a separate shared type that we add the info to, and have the callers create the appropriate bound nodes from it
            if (isUsingDeclaration)
            {
                return new BoundUsingLocalDeclarations(syntax, patternDisposeInfo, await, declarationsOpt, hasErrors);
            }
            else
            {
                BoundStatement boundBody = originalBinder.BindPossibleEmbeddedStatement(usingBinder!._syntax.Statement, diagnostics);

                return new BoundUsingStatement(
                    usingBinder._syntax,
                    usingBinder.Locals,
                    multipleDeclarations,
                    expression,
                    boundBody,
                    await,
                    patternDisposeInfo,
                    hasErrors);
            }

            bool bindDisposable(bool fromExpression, out MethodArgumentInfo? patternDisposeInfo, out TypeSymbol? awaitableType)
            {
                patternDisposeInfo = null;
                awaitableType = null;
                Debug.Assert(!fromExpression || expression != null);
                TypeSymbol? type = fromExpression ? expression!.Type : declarationType;

                // Pattern-based binding
                // If this is a ref struct, or we're in a valid asynchronous using, try binding via pattern.
                if (type is not null && (type.IsRefLikeType || hasAwait))
                {
                    BoundExpression? receiver = fromExpression
                                               ? expression
                                               : new BoundLocal(syntax, declarationsOpt[0].LocalSymbol, null, type) { WasCompilerGenerated = true };

                    BindingDiagnosticBag patternDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
                    MethodSymbol disposeMethod = originalBinder.TryFindDisposePatternMethod(receiver, syntax, hasAwait, patternDiagnostics, out bool expanded);
                    if (disposeMethod is not null)
                    {
                        diagnostics.AddRangeAndFree(patternDiagnostics);

                        var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(disposeMethod.ParameterCount);
                        ImmutableArray<int> argsToParams = default;

                        originalBinder.BindDefaultArguments(
                            // If this is a using statement, then we want to use the whole `using (expr) { }` as the argument location. These arguments
                            // will be represented in the IOperation tree and the "correct" node for them, given that they are an implicit invocation
                            // at the end of the using statement, is on the whole using statement, not on the current expression.
                            usingBinder?._syntax ?? syntax,
                            disposeMethod.Parameters,
                            argumentsBuilder,
                            argumentRefKindsBuilder: null,
                            namesBuilder: null,
                            ref argsToParams,
                            out BitVector defaultArguments,
                            expanded,
                            enableCallerInfo: true,
                            diagnostics);

                        Debug.Assert(argsToParams.IsDefault);
                        patternDisposeInfo = new MethodArgumentInfo(disposeMethod, argumentsBuilder.ToImmutableAndFree(), defaultArguments, expanded);
                        if (hasAwait)
                        {
                            awaitableType = disposeMethod.ReturnType;
                        }

                        return true;
                    }

                    patternDiagnostics.Free();
                }

                // Interface binding
                NamedTypeSymbol disposableInterface = getDisposableInterface(hasAwait);
                Debug.Assert(disposableInterface is not null);

                bool implementsIDisposable = implementsInterface(fromExpression, disposableInterface, diagnostics);

                if (implementsIDisposable)
                {
                    if (hasAwait)
                    {
                        awaitableType = originalBinder.Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask);
                    }

                    return !ReportUseSite(disposableInterface, diagnostics, hasAwait ? awaitKeyword : usingKeyword);
                }

                if (type is null || !type.IsErrorType())
                {
                    // Retry with a different assumption about whether the `using` is async
                    NamedTypeSymbol alternateInterface = getDisposableInterface(!hasAwait);
                    bool implementsAlternateIDisposable = implementsInterface(fromExpression, alternateInterface, BindingDiagnosticBag.Discarded);

                    ErrorCode errorCode = implementsAlternateIDisposable
                        ? (hasAwait ? ErrorCode.ERR_NoConvToIAsyncDispWrongAsync : ErrorCode.ERR_NoConvToIDispWrongAsync)
                        : (hasAwait ? ErrorCode.ERR_NoConvToIAsyncDisp : ErrorCode.ERR_NoConvToIDisp);

                    Error(diagnostics, errorCode, syntax, declarationType ?? expression!.Display);
                }

                return false;
            }

            bool implementsInterface(bool fromExpression, NamedTypeSymbol targetInterface, BindingDiagnosticBag diagnostics)
            {
                var conversions = originalBinder.Conversions;
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = originalBinder.GetNewCompoundUseSiteInfo(diagnostics);
                bool result;
                bool needSupportForRefStructInterfaces;

                if (fromExpression)
                {
                    Debug.Assert(expression is { });
                    result = conversions.HasImplicitConversionToOrImplementsVarianceCompatibleInterface(expression, targetInterface, ref useSiteInfo, out needSupportForRefStructInterfaces);
                }
                else
                {
                    Debug.Assert(declarationType is { });
                    result = conversions.HasImplicitConversionToOrImplementsVarianceCompatibleInterface(declarationType, targetInterface, ref useSiteInfo, out needSupportForRefStructInterfaces);
                }

                diagnostics.Add(syntax, useSiteInfo);

                return result;
            }

            NamedTypeSymbol getDisposableInterface(bool isAsync)
            {
                return isAsync
                    ? originalBinder.Compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable)
                    : originalBinder.Compilation.GetSpecialType(SpecialType.System_IDisposable);
            }
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (_syntax == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _syntax;
            }
        }
    }
}
