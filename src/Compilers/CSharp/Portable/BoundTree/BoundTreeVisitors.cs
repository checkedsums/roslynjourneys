// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using Roslyn.Utilities;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundTreeVisitor<A, R>
    {
        protected BoundTreeVisitor()
        {
        }

        public virtual R Visit(BoundNode node, A arg)
        {
            if (node == null)
            {
                return default;
            }

            // this switch contains fewer than 50 of the most common node kinds
            return node.Kind switch
            {
                BoundKind.TypeExpression => VisitTypeExpression(node as BoundTypeExpression, arg),
                BoundKind.NamespaceExpression => VisitNamespaceExpression(node as BoundNamespaceExpression, arg),
                BoundKind.UnaryOperator => VisitUnaryOperator(node as BoundUnaryOperator, arg),
                BoundKind.IncrementOperator => VisitIncrementOperator(node as BoundIncrementOperator, arg),
                BoundKind.BinaryOperator => VisitBinaryOperator(node as BoundBinaryOperator, arg),
                BoundKind.CompoundAssignmentOperator => VisitCompoundAssignmentOperator(node as BoundCompoundAssignmentOperator, arg),
                BoundKind.AssignmentOperator => VisitAssignmentOperator(node as BoundAssignmentOperator, arg),
                BoundKind.NullCoalescingOperator => VisitNullCoalescingOperator(node as BoundNullCoalescingOperator, arg),
                BoundKind.ConditionalOperator => VisitConditionalOperator(node as BoundConditionalOperator, arg),
                BoundKind.ArrayAccess => VisitArrayAccess(node as BoundArrayAccess, arg),
                BoundKind.TypeOfOperator => VisitTypeOfOperator(node as BoundTypeOfOperator, arg),
                BoundKind.DefaultLiteral => VisitDefaultLiteral(node as BoundDefaultLiteral, arg),
                BoundKind.DefaultExpression => VisitDefaultExpression(node as BoundDefaultExpression, arg),
                BoundKind.IsOperator => VisitIsOperator(node as BoundIsOperator, arg),
                BoundKind.AsOperator => VisitAsOperator(node as BoundAsOperator, arg),
                BoundKind.Conversion => VisitConversion(node as BoundConversion, arg),
                BoundKind.SequencePointExpression => VisitSequencePointExpression(node as BoundSequencePointExpression, arg),
                BoundKind.SequencePoint => VisitSequencePoint(node as BoundSequencePoint, arg),
                BoundKind.SequencePointWithSpan => VisitSequencePointWithSpan(node as BoundSequencePointWithSpan, arg),
                BoundKind.Block => VisitBlock(node as BoundBlock, arg),
                BoundKind.LocalDeclaration => VisitLocalDeclaration(node as BoundLocalDeclaration, arg),
                BoundKind.MultipleLocalDeclarations => VisitMultipleLocalDeclarations(node as BoundMultipleLocalDeclarations, arg),
                BoundKind.Sequence => VisitSequence(node as BoundSequence, arg),
                BoundKind.NoOpStatement => VisitNoOpStatement(node as BoundNoOpStatement, arg),
                BoundKind.ReturnStatement => VisitReturnStatement(node as BoundReturnStatement, arg),
                BoundKind.ThrowStatement => VisitThrowStatement(node as BoundThrowStatement, arg),
                BoundKind.ExpressionStatement => VisitExpressionStatement(node as BoundExpressionStatement, arg),
                BoundKind.BreakStatement => VisitBreakStatement(node as BoundBreakStatement, arg),
                BoundKind.ContinueStatement => VisitContinueStatement(node as BoundContinueStatement, arg),
                BoundKind.IfStatement => VisitIfStatement(node as BoundIfStatement, arg),
                BoundKind.ForEachStatement => VisitForEachStatement(node as BoundForEachStatement, arg),
                BoundKind.TryStatement => VisitTryStatement(node as BoundTryStatement, arg),
                BoundKind.Literal => VisitLiteral(node as BoundLiteral, arg),
                BoundKind.ThisReference => VisitThisReference(node as BoundThisReference, arg),
                BoundKind.Local => VisitLocal(node as BoundLocal, arg),
                BoundKind.Parameter => VisitParameter(node as BoundParameter, arg),
                BoundKind.LabelStatement => VisitLabelStatement(node as BoundLabelStatement, arg),
                BoundKind.GotoStatement => VisitGotoStatement(node as BoundGotoStatement, arg),
                BoundKind.LabeledStatement => VisitLabeledStatement(node as BoundLabeledStatement, arg),
                BoundKind.StatementList => VisitStatementList(node as BoundStatementList, arg),
                BoundKind.ConditionalGoto => VisitConditionalGoto(node as BoundConditionalGoto, arg),
                BoundKind.Call => VisitCall(node as BoundCall, arg),
                BoundKind.ObjectCreationExpression => VisitObjectCreationExpression(node as BoundObjectCreationExpression, arg),
                BoundKind.DelegateCreationExpression => VisitDelegateCreationExpression(node as BoundDelegateCreationExpression, arg),
                BoundKind.FieldAccess => VisitFieldAccess(node as BoundFieldAccess, arg),
                BoundKind.PropertyAccess => VisitPropertyAccess(node as BoundPropertyAccess, arg),
                BoundKind.Lambda => VisitLambda(node as BoundLambda, arg),
                BoundKind.NameOfOperator => VisitNameOfOperator(node as BoundNameOfOperator, arg),
                _ => VisitInternal(node, arg),
            };
        }

        public virtual R DefaultVisit(BoundNode node, A arg)
        {
            return default;
        }
    }

    internal abstract partial class BoundTreeVisitor
    {
        protected BoundTreeVisitor()
        {
        }

        [DebuggerHidden]
        public virtual BoundNode Visit(BoundNode node)
        {
            if (node != null)
            {
                return node.Accept(this);
            }

            return null;
        }

        [DebuggerHidden]
        public virtual BoundNode DefaultVisit(BoundNode node)
        {
            return null;
        }

        public class CancelledByStackGuardException : Exception
        {
            public readonly BoundNode Node;

            public CancelledByStackGuardException(Exception inner, BoundNode node)
                : base(inner.Message, inner)
            {
                Node = node;
            }

            public void AddAnError(DiagnosticBag diagnostics)
            {
                diagnostics.Add(ErrorCode.ERR_InsufficientStack, GetTooLongOrComplexExpressionErrorLocation(Node));
            }

            public void AddAnError(BindingDiagnosticBag diagnostics)
            {
                diagnostics.Add(ErrorCode.ERR_InsufficientStack, GetTooLongOrComplexExpressionErrorLocation(Node));
            }

            public static Location GetTooLongOrComplexExpressionErrorLocation(BoundNode node)
            {
                SyntaxNode syntax = node.Syntax;

                if (syntax is not (ExpressionSyntax or PatternSyntax))
                {
                    syntax = syntax.DescendantNodes(n => n is not (ExpressionSyntax or PatternSyntax)).FirstOrDefault(n => n is ExpressionSyntax or PatternSyntax) ?? syntax;
                }

                return syntax.GetFirstToken().GetLocation();
            }
        }

        /// <summary>
        /// Consumers must provide implementation for <see cref="VisitExpressionOrPatternWithoutStackGuard"/>.
        /// </summary>
        [DebuggerStepThrough]
        protected BoundNode VisitExpressionOrPatternWithStackGuard(ref int recursionDepth, BoundNode node)
        {
            Debug.Assert(node is BoundExpression or BoundPattern);
            BoundNode result;
            recursionDepth++;
#if DEBUG
            int saveRecursionDepth = recursionDepth;
#endif

            if (recursionDepth > 1 || !ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException())
            {
                EnsureSufficientExecutionStack(recursionDepth);

                result = VisitExpressionOrPatternWithoutStackGuard(node);
            }
            else
            {
                result = VisitExpressionOrPatternWithStackGuard(node);
            }

#if DEBUG
            Debug.Assert(saveRecursionDepth == recursionDepth);
#endif
            recursionDepth--;
            return result;
        }

        protected virtual void EnsureSufficientExecutionStack(int recursionDepth)
        {
            StackGuard.EnsureSufficientExecutionStack(recursionDepth);
        }

        protected virtual bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return true;
        }

#nullable enable
        [DebuggerStepThrough]
        private BoundNode? VisitExpressionOrPatternWithStackGuard(BoundNode node)
        {
            try
            {
                return VisitExpressionOrPatternWithoutStackGuard(node);
            }
            catch (InsufficientExecutionStackException ex)
            {
                throw new CancelledByStackGuardException(ex, node);
            }
        }

        /// <summary>
        /// We should be intentional about behavior of derived classes regarding guarding against stack overflow.
        /// </summary>
        protected abstract BoundNode? VisitExpressionOrPatternWithoutStackGuard(BoundNode node);
    }
}
