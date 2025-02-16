﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SyntaxNodeExtensions
    {
        public static TNode WithAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : CSharpSyntaxNode
        {
            return (TNode)node.Green.SetAnnotations(annotations).CreateRed();
        }

        public static bool IsAnonymousFunction(this SyntaxNode syntax)
        {
            Debug.Assert(syntax != null);
            return syntax.Kind() switch
            {
                SyntaxKind.ParenthesizedLambdaExpression or SyntaxKind.SimpleLambdaExpression or SyntaxKind.AnonymousMethodExpression => true,
                _ => false,
            };
        }

        public static bool IsQuery(this SyntaxNode syntax)
            => syntax.Kind() is SyntaxKind.FromClause or SyntaxKind.GroupClause or SyntaxKind.JoinClause or SyntaxKind.JoinIntoClause or SyntaxKind.LetClause or SyntaxKind.OrderByClause or SyntaxKind.QueryContinuation or SyntaxKind.QueryExpression or SyntaxKind.SelectClause or SyntaxKind.WhereClause;

        internal static bool MayBeNameofOperator(this InvocationExpressionSyntax node)
        {
            if (node.Expression.Kind() == SyntaxKind.IdentifierName &&
                ((IdentifierNameSyntax)node.Expression).Identifier.ContextualKind() == SyntaxKind.NameOfKeyword &&
                node.ArgumentList.Arguments.Count == 1)
            {
                ArgumentSyntax argument = node.ArgumentList.Arguments[0];
                if (argument.NameColon == null && argument.RefOrOutKeyword == default)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This method is used to keep the code that generates binders in sync
        /// with the code that searches for binders.  We don't want the searcher
        /// to skip over any nodes that could have associated binders, especially
        /// if changes are made later.
        /// 
        /// "Local binder" is a term that refers to binders that are
        /// created by LocalBinderFactory.
        /// </summary>
        internal static bool CanHaveAssociatedLocalBinder(this SyntaxNode syntax) => syntax.Kind() switch
        {
            SyntaxKind.InvocationExpression when ((InvocationExpressionSyntax)syntax).MayBeNameofOperator() => true,
            SyntaxKind.CatchClause or SyntaxKind.ParenthesizedLambdaExpression or SyntaxKind.SimpleLambdaExpression or SyntaxKind.AnonymousMethodExpression or SyntaxKind.CatchFilterClause or SyntaxKind.SwitchSection or SyntaxKind.EqualsValueClause or SyntaxKind.Attribute or SyntaxKind.ArgumentList or SyntaxKind.ArrowExpressionClause or SyntaxKind.SwitchExpression or SyntaxKind.SwitchExpressionArm or SyntaxKind.BaseConstructorInitializer or SyntaxKind.ThisConstructorInitializer or SyntaxKind.ConstructorDeclaration or SyntaxKind.PrimaryConstructorBaseType or SyntaxKind.CheckedExpression or SyntaxKind.UncheckedExpression => true,
            SyntaxKind.RecordStructDeclaration => false,
            _ => syntax is StatementSyntax || IsValidScopeDesignator(syntax as ExpressionSyntax),
        };

        internal static bool IsValidScopeDesignator(this ExpressionSyntax? expression) => expression?.Parent?.Kind() switch // All these nodes are valid scope designators due to the pattern matching and out vars features.
        {
            SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression => ((LambdaExpressionSyntax)expression?.Parent!).Body == expression,
            SyntaxKind.SwitchStatement => ((SwitchStatementSyntax)expression?.Parent!).Expression == expression,
            SyntaxKind.ForStatement => ((ForStatementSyntax)expression?.Parent!).Condition == expression || ((ForStatementSyntax)expression?.Parent!).Incrementors.FirstOrDefault() == expression,
            SyntaxKind.ForEachStatement or SyntaxKind.ForEachVariableStatement => ((CommonForEachStatementSyntax)expression?.Parent!).Expression == expression,
            _ or null => false,
        };

        internal static CSharpSyntaxNode AnonymousFunctionBody(this SyntaxNode lambda) => ((AnonymousFunctionExpressionSyntax)lambda).Body;

        /// <summary>
        /// Given an initializer expression infer the name of anonymous property or tuple element.
        /// Returns default if unsuccessful
        /// </summary>
        internal static SyntaxToken ExtractAnonymousTypeMemberName(this ExpressionSyntax input)
        {
            while (true)
            {
                switch (input.Kind())
                {
                    case SyntaxKind.IdentifierName:
                        return ((IdentifierNameSyntax)input).Identifier;

                    case SyntaxKind.SimpleMemberAccessExpression:
                        input = ((MemberAccessExpressionSyntax)input).Name;
                        continue;

                    case SyntaxKind.ConditionalAccessExpression:
                        input = ((ConditionalAccessExpressionSyntax)input).WhenNotNull;
                        if (input.IsKind(SyntaxKind.MemberBindingExpression))
                            return ((MemberBindingExpressionSyntax)input).Name.Identifier;

                        continue;

                    default:
                        return default;
                }
            }
        }

        internal static RefKind GetRefKindInLocalOrReturn(this TypeSyntax syntax, BindingDiagnosticBag diagnostics)
        {
            syntax.SkipRefInLocalOrReturn(diagnostics, out var refKind);
            return refKind;
        }

        /// <summary>
        /// For callers that just want to unwrap a <see cref="RefTypeSyntax"/> and don't care if ref/readonly was there.
        /// As these callers don't care about 'ref', they are in scenarios where 'ref' is not legal, and existing code
        /// will error out for them.  Callers that do want to know what the ref-kind is should use <see
        /// cref="SkipRefInLocalOrReturn"/> or <see cref="SkipRefInField"/> depending on which language feature they are
        /// asking for.
        /// </summary>
        internal static TypeSyntax SkipRef(this TypeSyntax syntax) => SkipRefWorker(syntax, diagnostics: null, out _);
        // Intentionally pass no diagnostics here.  This is for ref-fields which handles all its diagnostics itself in the field symbol.
        internal static TypeSyntax SkipRefInField(this TypeSyntax syntax, out RefKind refKind) => SkipRefWorker(syntax, diagnostics: null, out refKind);

        internal static TypeSyntax SkipRefInLocalOrReturn(this TypeSyntax syntax, BindingDiagnosticBag? diagnostics, out RefKind refKind) => SkipRefWorker(syntax, diagnostics, out refKind);

        private static TypeSyntax SkipRefWorker(TypeSyntax syntax, BindingDiagnosticBag? diagnostics, out RefKind refKind)
        {
            if (syntax.Kind() == SyntaxKind.RefType)
            {
                var refType = (RefTypeSyntax)syntax;
                refKind = refType.ReadOnlyKeyword.Kind() == SyntaxKind.ReadOnlyKeyword
                    ? RefKind.RefReadOnly
                    : RefKind.Ref;

                if (diagnostics != null)
                {
#if DEBUG
                    var current = syntax;
                    if (current.Parent is ScopedTypeSyntax scopedType)
                        current = scopedType;

                    // Should only be called with diagnostics from a location where we're a return-type or local-type.
                    Debug.Assert(
                        (current.Parent is ParenthesizedLambdaExpressionSyntax lambda && lambda.ReturnType == current) ||
                        (current.Parent is LocalFunctionStatementSyntax localFunction && localFunction.ReturnType == current) ||
                        (current.Parent is MethodDeclarationSyntax method && method.ReturnType == current) ||
                        (current.Parent is BasePropertyDeclarationSyntax property && property.Type == current) ||
                        (current.Parent is DelegateDeclarationSyntax delegateDeclaration && delegateDeclaration.ReturnType == current) ||
                        (current.Parent is VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax } variableDeclaration && variableDeclaration.Type == current));
#endif
                }

                return refType.Type;
            }

            refKind = RefKind.None;
            return syntax;
        }

        internal static TypeSyntax SkipScoped(this TypeSyntax syntax, out bool isScoped)
        {
            if (syntax is ScopedTypeSyntax scopedType)
            {
                isScoped = true;
                return scopedType.Type;
            }

            isScoped = false;
            return syntax;
        }

        internal static SyntaxNode ModifyingScopedOrRefTypeOrSelf(this SyntaxNode syntax)
        {
            SyntaxNode? parentNode = syntax.Parent;

            if (parentNode is RefTypeSyntax refType && refType.Type == syntax)
            {
                syntax = refType;
                parentNode = parentNode.Parent;
            }

            return parentNode is not ScopedTypeSyntax scopedType || scopedType.Type != syntax ? syntax : scopedType;
        }

        internal static ExpressionSyntax? UnwrapRefExpression(
            this ExpressionSyntax? syntax,
            BindingDiagnosticBag diagnostics,
            out RefKind refKind)
        {
            if (syntax is not RefExpressionSyntax { Expression: var expression } refExpression)
            {
                refKind = RefKind.None;
                return syntax;
            }

            refKind = RefKind.Ref;
            expression.CheckDeconstructionCompatibleArgument(diagnostics);
            return expression;
        }

        internal static void CheckDeconstructionCompatibleArgument(this ExpressionSyntax expression, BindingDiagnosticBag diagnostics)
        {
            if (IsDeconstructionCompatibleArgument(expression))
            {
                diagnostics.Add(ErrorCode.ERR_VarInvocationLvalueReserved, expression.GetLocation());
            }
        }

        /// <summary>
        /// See if the expression is an invocation of a method named 'var',
        /// I.e. something like "var(x, y)" or "var(x, (y, z))" or "var(1)".
        /// We report an error when such an invocation is used in a certain syntactic contexts that
        /// will require an lvalue because we may elect to support deconstruction
        /// in the future. We need to ensure that we do not successfully interpret this as an invocation of a
        /// ref-returning method named var.
        /// </summary>
        private static bool IsDeconstructionCompatibleArgument(ExpressionSyntax expression)
        {
            if (expression.Kind() == SyntaxKind.InvocationExpression)
            {
                var invocation = (InvocationExpressionSyntax)expression;
                var invocationTarget = invocation.Expression;

                return invocationTarget.Kind() == SyntaxKind.IdentifierName &&
                    ((IdentifierNameSyntax)invocationTarget).IsVar;
            }

            return false;
        }

        internal static SimpleNameSyntax? GetInterceptableNameSyntax(this InvocationExpressionSyntax invocation)
        {
            // If a qualified name is used as a valid receiver of an invocation syntax at some point,
            // we probably want to treat it similarly to a MemberAccessExpression.
            // However, we don't expect to encounter it.
            Debug.Assert(invocation.Expression is not QualifiedNameSyntax);

            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
                SimpleNameSyntax name => name,
                _ => null
            };
        }
    }
}
