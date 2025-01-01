// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable format, IDE1006

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFacts
    {
        /// <summary>
        /// Returns true if the node is the alias of an AliasQualifiedNameSyntax
        /// </summary>
        public static bool IsAliasQualifier(SyntaxNode node)
            => node.Parent is AliasQualifiedNameSyntax p && p.Alias == node;

        public static bool IsAttributeName(SyntaxNode node)
            => (node.Parent is null || !IsName(node.Kind())) && node.Parent?.Kind() switch
            {
                QualifiedName => ((QualifiedNameSyntax)node.Parent).Right == node && IsAttributeName(node.Parent),
                AliasQualifiedName => ((AliasQualifiedNameSyntax)node.Parent).Name == node && IsAttributeName(node.Parent),
                _ or null => node?.Parent is AttributeSyntax p && p.Name == node,
            };

        /// <summary>
        /// Returns true if the node is the object of an invocation expression.
        /// </summary>
        public static bool IsInvoked(ExpressionSyntax node)
        {
            node = SyntaxFactory.GetStandaloneExpression(node);
            return node.Parent is InvocationExpressionSyntax inv && inv.Expression == node;
        }

        /// <summary>
        /// Returns true if the node is the object of an element access expression.
        /// </summary>
        public static bool IsIndexed(ExpressionSyntax node)
        {
            node = SyntaxFactory.GetStandaloneExpression(node);
            return node.Parent is ElementAccessExpressionSyntax indexer && indexer.Expression == node;
        }

        public static bool IsNamespaceAliasQualifier(ExpressionSyntax node)
            => node.Parent is AliasQualifiedNameSyntax parent && parent.Alias == node;

        /// <summary>
        /// Returns true if the node is in a tree location that is expected to be a type
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsInTypeOnlyContext(ExpressionSyntax node)
        {
            node = SyntaxFactory.GetStandaloneExpression(node);
            var parent = node.Parent;
            return parent is not null && (parent.IsKind(FunctionPointerType) ? throw ExceptionUtilities.Unreachable() : parent.Kind() switch
            {
                Attribute => ((AttributeSyntax)parent).Name == node,
                ArrayType => ((ArrayTypeSyntax)parent).ElementType == node,
                PointerType => ((PointerTypeSyntax)parent).ElementType == node,
                SimpleBaseType or CrefParameter or PredefinedType or TypeArgumentList => true, // all children of GenericNames are type arguments
                NullableType => ((NullableTypeSyntax)parent).ElementType == node,
                CastExpression => ((CastExpressionSyntax)parent).Type == node,
                ObjectCreationExpression => ((ObjectCreationExpressionSyntax)parent).Type == node,
                StackAllocArrayCreationExpression => ((StackAllocArrayCreationExpressionSyntax)parent).Type == node,
                FromClause => ((FromClauseSyntax)parent).Type == node,
                JoinClause => ((JoinClauseSyntax)parent).Type == node,
                VariableDeclaration => ((VariableDeclarationSyntax)parent).Type == node,
                ForEachStatement => ((ForEachStatementSyntax)parent).Type == node,
                CatchDeclaration => ((CatchDeclarationSyntax)parent).Type == node,
                AsExpression or IsExpression => ((BinaryExpressionSyntax)parent).Right == node,
                TypeOfExpression => ((TypeOfExpressionSyntax)parent).Type == node,
                SizeOfExpression => ((SizeOfExpressionSyntax)parent).Type == node,
                DefaultExpression => ((DefaultExpressionSyntax)parent).Type == node,
                RefValueExpression => ((RefValueExpressionSyntax)parent).Type == node,
                RefType => ((RefTypeSyntax)parent).Type == node,
                ScopedType => ((ScopedTypeSyntax)parent).Type == node,
                Parameter or FunctionPointerParameter => ((BaseParameterSyntax)parent).Type == node,
                TypeConstraint => ((TypeConstraintSyntax)parent).Type == node,
                MethodDeclaration => ((MethodDeclarationSyntax)parent).ReturnType == node,
                IndexerDeclaration => ((IndexerDeclarationSyntax)parent).Type == node,
                OperatorDeclaration => ((OperatorDeclarationSyntax)parent).ReturnType == node,
                ConversionOperatorDeclaration => ((ConversionOperatorDeclarationSyntax)parent).Type == node,
                PropertyDeclaration => ((PropertyDeclarationSyntax)parent).Type == node,
                DelegateDeclaration => ((DelegateDeclarationSyntax)parent).ReturnType == node,
                EventDeclaration => ((EventDeclarationSyntax)parent).Type == node,
                LocalFunctionStatement => ((LocalFunctionStatementSyntax)parent).ReturnType == node,
                ParenthesizedLambdaExpression => ((ParenthesizedLambdaExpressionSyntax)parent).ReturnType == node,
                PrimaryConstructorBaseType => ((PrimaryConstructorBaseTypeSyntax)parent).Type == node,
                ConversionOperatorMemberCref => ((ConversionOperatorMemberCrefSyntax)parent).Type == node,
                ExplicitInterfaceSpecifier => ((ExplicitInterfaceSpecifierSyntax)parent).Name == node,// #13.4.1 An explicit member implementation is a method, property, event or indexer declaration that references a fully qualified interface member name.
                DeclarationPattern => ((DeclarationPatternSyntax)parent).Type == node,                // A ExplicitInterfaceSpecifier represents the left part (QN) of the member name, so it should be treated like a QualifiedName.
                RecursivePattern => ((RecursivePatternSyntax)parent).Type == node,
                TupleElement => ((TupleElementSyntax)parent).Type == node,
                DeclarationExpression => ((DeclarationExpressionSyntax)parent).Type == node,
                IncompleteMember => ((IncompleteMemberSyntax)parent).Type == node,
                TypePattern => ((TypePatternSyntax)parent).Type == node,
                _ => false,
            });
        }

        /// <summary>
        /// Returns true if a node is in a tree location that is expected to be either a namespace or type
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsInNamespaceOrTypeContext(ExpressionSyntax? node)
        {
            if (node is not null)
            {
                node = SyntaxFactory.GetStandaloneExpression(node);
                var parent = node.Parent;

                return (parent is not null) && parent.Kind() switch
                {
                    UsingDirective => ((UsingDirectiveSyntax)parent).NamespaceOrType == node,
                    QualifiedName => ((QualifiedNameSyntax)parent).Left == node,// left of QN is namespace or type.  Note: when you have "a.b.c()", then "a.b" is not a qualified name,
                    _ => IsInTypeOnlyContext(node),                             // it is a member access expression. Qualified names are only parsed when the parser knows it's a type only context.
                };
            }

            return false;
        }

        /// <summary>
        /// Is the node the name of a named argument of an invocation, object creation expression, 
        /// constructor initializer, or element access, but not an attribute.
        /// </summary>
        public static bool IsNamedArgumentName(SyntaxNode node)
        {
            // An argument name is an IdentifierName inside a NameColon, inside an Argument, inside an ArgumentList, inside an
            // Invocation, ObjectCreation, ObjectInitializer, ElementAccess or Subpattern.

            return node.IsKind(IdentifierName) && node.Parent is not null && node.Parent.IsKind(NameColon) && node.Parent.Parent.IsKind(Subpattern) && node.Parent.Parent.Kind() is Argument or AttributeArgument && node.Parent.Parent.Parent is not null
                && node.Parent.Parent.Parent.IsKind(TupleExpression) && (node.Parent.Parent.Parent is BaseArgumentListSyntax || node.Parent.Parent.Parent.IsKind(AttributeArgumentList)) && node.Parent.Parent.Parent.Parent?.Kind() switch
                {
                    InvocationExpression or TupleExpression or ObjectCreationExpression or ImplicitObjectCreationExpression or
                    ObjectInitializerExpression or ElementAccessExpression or Attribute or BaseConstructorInitializer or ThisConstructorInitializer or PrimaryConstructorBaseType => true,
                    _ or null => false,
                };
        }

        /// <summary>
        /// Is the expression the initializer in a fixed statement?
        /// </summary>
        public static bool IsFixedStatementExpression(SyntaxNode node)
        {
            var current = node.Parent;
            // Dig through parens because dev10 does (even though the spec doesn't say so)
            // Dig through casts because there's a special error code (CS0254) for such casts.
            while (current is not null && current.Kind() is ParenthesizedExpression or CastExpression) current = current.Parent;
            if (current is null || current.Kind() is not EqualsValueClause) return false; current = current.Parent;
            if (current is null || current.Kind() is not VariableDeclarator) return false; current = current.Parent;
            if (current is null || current.Kind() is not VariableDeclaration) return false; current = current.Parent;
            return current is not null && current.IsKind(FixedStatement);
        }

        public static string GetText(Accessibility accessibility)
            => accessibility switch {
                Accessibility.ProtectedAndInternal => GetText(PrivateKeyword) + " " + GetText(ProtectedKeyword),
                Accessibility.ProtectedOrInternal => GetText(ProtectedKeyword) + " " + GetText(InternalKeyword),
                Accessibility.NotApplicable => string.Empty,
                _ => GetText(accessibility switch
                {
                    Accessibility.Private => PrivateKeyword,
                    Accessibility.Internal => InternalKeyword,
                    Accessibility.Protected => ProtectedKeyword,
                    Accessibility.Public => PublicKeyword,
                    _ => throw ExceptionUtilities.UnexpectedValue(accessibility),
                }),
            };

        /* 
        // The grammar gives:
        //
        // expression-statement:
        //     statement-expression ;
        //
        // statement-expression:
        //     invocation-expression
        //     object-creation-expression
        //     assignment
        //     post-increment-expression
        //     post-decrement-expression
        //     pre-increment-expression
        //     pre-decrement-expression
        //     await-expression
        */
        internal static bool IsStatementExpression(SyntaxNode syntax) =>
            syntax.Kind() switch
            {
                InvocationExpression or ObjectCreationExpression or SimpleAssignmentExpression or AddAssignmentExpression or SubtractAssignmentExpression or MultiplyAssignmentExpression or DivideAssignmentExpression or ModuloAssignmentExpression or AndAssignmentExpression or OrAssignmentExpression or ExclusiveOrAssignmentExpression or LeftShiftAssignmentExpression or RightShiftAssignmentExpression or UnsignedRightShiftAssignmentExpression or CoalesceAssignmentExpression or PostIncrementExpression or PostDecrementExpression or PreIncrementExpression or PreDecrementExpression or AwaitExpression => true,
                ConditionalAccessExpression => IsStatementExpression(((ConditionalAccessExpressionSyntax)syntax).WhenNotNull),
                // Allow missing IdentifierNames; they will show up in error cases
                // where there is no statement whatsoever.
                IdentifierName => syntax.IsMissing,
                _ => false,
            };

        internal static bool IsIdentifierVar(this Syntax.InternalSyntax.SyntaxToken node) 
            => node.ContextualKind == VarKeyword;

        internal static bool IsDeclarationExpressionType(SyntaxNode node, [NotNullWhen(true)] out DeclarationExpressionSyntax? parent) 
            => node == (parent = node.ModifyingScopedOrRefTypeOrSelf().Parent as DeclarationExpressionSyntax)?.Type.SkipScoped(out _).SkipRef();

        /// <summary>
        /// Given an initializer expression infer the name of anonymous property or tuple element.
        /// Returns null if unsuccessful
        /// </summary>
        public static string? TryGetInferredMemberName(this SyntaxNode syntax)
        {
            SyntaxToken nameToken;
            switch (syntax.Kind())
            {
                case SingleVariableDesignation:
                    nameToken = ((SingleVariableDesignationSyntax)syntax).Identifier;
                    break;

                case DeclarationExpression:
                    var declaration = (DeclarationExpressionSyntax)syntax;
                    var designationKind = declaration.Designation.Kind();
                    if (designationKind == ParenthesizedVariableDesignation ||
                        designationKind == DiscardDesignation)
                    {
                        return null;
                    }

                    nameToken = ((SingleVariableDesignationSyntax)declaration.Designation).Identifier;
                    break;

                case ParenthesizedVariableDesignation:
                case DiscardDesignation:
                    return null;

                default:
                    if (syntax is ExpressionSyntax expr)
                    {
                        nameToken = expr.ExtractAnonymousTypeMemberName();
                        break;
                    }
                    return null;
            }

            return nameToken.RawKind != 0 ? nameToken.ValueText : null;
        }

        /// <summary>
        /// Checks whether the element name is reserved.
        ///
        /// For example:
        /// "Item3" is reserved (at certain positions).
        /// "Rest", "ToString" and other members of System.ValueTuple are reserved (in any position).
        /// Names that are not reserved return false.
        /// </summary>
        public static bool IsReservedTupleElementName(string elementName)
            => NamedTypeSymbol.IsTupleElementNameReserved(elementName) != -1;

        internal static bool HasAnyBody(this BaseMethodDeclarationSyntax declaration) 
            => (declaration.Body ?? (SyntaxNode?)declaration.ExpressionBody) is not null;

        internal static bool IsExpressionBodied(this BaseMethodDeclarationSyntax declaration)
            => declaration.Body == null && declaration.ExpressionBody != null;

        internal static bool IsVarArg(this BaseMethodDeclarationSyntax declaration) => IsVarArg(declaration.ParameterList);
        internal static bool IsVarArg(this ParameterListSyntax parameterList) => parameterList.Parameters.Any(static p => p.IsArgList);

        internal static bool IsTopLevelStatement([NotNullWhen(true)] GlobalStatementSyntax? syntax)
            => syntax?.Parent is not null && syntax.Parent.IsKind(CompilationUnit);

        internal static bool IsSimpleProgramTopLevelStatement(GlobalStatementSyntax? syntax) 
            => IsTopLevelStatement(syntax) && syntax.SyntaxTree.Options.Kind is SourceCodeKind.Regular;

        internal static bool HasAwaitOperations(SyntaxNode node) // Do not descend into functions
            => node.DescendantNodesAndSelf(child => !IsNestedFunction(child)).Any(
                static _node => _node switch
                {
                    LocalDeclarationStatementSyntax local => local.AwaitKeyword.IsKind(AwaitKeyword),
                    CommonForEachStatementSyntax @foreach => @foreach.AwaitKeyword.IsKind(AwaitKeyword),
                    UsingStatementSyntax @using => @using.AwaitKeyword.IsKind(AwaitKeyword),
                    _ or null => _node is AwaitExpressionSyntax,
                });

        private static bool IsNestedFunction(SyntaxNode child) => IsNestedFunction(child.Kind());
        private static bool IsNestedFunction(SyntaxKind kind) => kind is LocalFunctionStatement or AnonymousMethodExpression or SimpleLambdaExpression or ParenthesizedLambdaExpression;

        [PerformanceSensitive("https://github.com/dotnet/roslyn/pull/66970", Constraint = "Use Green nodes for walking to avoid heavy allocations.")]
        internal static bool HasYieldOperations(SyntaxNode? node)
        {
            if (node is null)
                return false;

            var stack = ArrayBuilder<GreenNode>.GetInstance();
            stack.Push(node.Green);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                Debug.Assert(node.Green == current || current is not Syntax.InternalSyntax.MemberDeclarationSyntax and not Syntax.InternalSyntax.TypeDeclarationSyntax);

                if (current is null)
                    continue;

                // Do not descend into functions and expressions
                if (IsNestedFunction((SyntaxKind)current.RawKind) ||
                    current is Syntax.InternalSyntax.ExpressionSyntax)
                {
                    continue;
                }

                if (current is Syntax.InternalSyntax.YieldStatementSyntax)
                {
                    stack.Free();
                    return true;
                }

                foreach (var child in current.ChildNodesAndTokens())
                {
                    if (!child.IsToken)
                        stack.Push(child);
                }
            }

            stack.Free();
            return false;
        }

        internal static bool HasReturnWithExpression(SyntaxNode? node)
        {
            // Do not descend into functions and expressions
            return node is not null &&
                   node.DescendantNodesAndSelf(child => !IsNestedFunction(child) && !(node is ExpressionSyntax)).Any(n => n is ReturnStatementSyntax { Expression: { } });
        }
    }
}
