// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class AbstractFlowPass<TLocalState, TLocalFunctionState>
    {
        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            // dispatch to the switch sections
            var afterSwitchState = VisitSwitchStatementDispatch(node);

            // visit switch sections
            var switchSections = node.SwitchSections;
            var iLastSection = (switchSections.Length - 1);
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                VisitSwitchSection(switchSections[iSection], iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                Join(ref afterSwitchState, ref this.State);
            }

            ResolveBreaks(afterSwitchState, node.BreakLabel);

            return null!;
        }

        protected virtual TLocalState VisitSwitchStatementDispatch(BoundSwitchStatement node)
        {
            // visit switch header
            VisitRvalue(node.Expression);

            TLocalState initialState = this.State.Clone();

            var reachableLabels = node.ReachabilityDecisionDag.ReachableLabels;
            foreach (var section in node.SwitchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    if (reachableLabels.Contains(label.Label) || label.HasErrors)
                    {
                        SetState(initialState.Clone());
                    }
                    else
                    {
                        SetUnreachable();
                    }

                    VisitPattern(label.Pattern);
                    SetState(StateWhenTrue);
                    if (label.WhenClause != null)
                    {
                        VisitCondition(label.WhenClause);
                        SetState(StateWhenTrue);
                    }

                    PendingBranches.Add(new PendingBranch(label, this.State, label.Label));
                }
            }

            TLocalState afterSwitchState = UnreachableState();
            if (node.ReachabilityDecisionDag.ReachableLabels.Contains(node.BreakLabel))
            {
                Join(ref afterSwitchState, ref initialState);
            }

            return afterSwitchState;
        }

        protected virtual void VisitSwitchSection(BoundSwitchSection node, bool isLastSection)
        {
            SetState(UnreachableState());
            foreach (var label in node.SwitchLabels)
            {
                VisitLabel(label.Label, node);
            }

            VisitStatementList(node);
        }

        public override BoundNode VisitSwitchDispatch(BoundSwitchDispatch node)
        {
            VisitRvalue(node.Expression);
            var state = this.State.Clone();
            PendingBranches.Add(new PendingBranch(node, state, node.DefaultLabel));
            foreach ((_, LabelSymbol label) in node.Cases)
            {
                PendingBranches.Add(new PendingBranch(node, state, label));
            }

            SetUnreachable();
            return null!;
        }

        public override BoundNode VisitConvertedSwitchExpression(BoundConvertedSwitchExpression node)
        {
            return this.VisitSwitchExpression(node);
        }

        public override BoundNode VisitUnconvertedSwitchExpression(BoundUnconvertedSwitchExpression node)
        {
            return this.VisitSwitchExpression(node);
        }

        private BoundNode VisitSwitchExpression(BoundSwitchExpression node)
        {
            VisitRvalue(node.Expression);
            var dispatchState = this.State;
            var endState = UnreachableState();
            var reachableLabels = node.ReachabilityDecisionDag.ReachableLabels;
            foreach (var arm in node.SwitchArms)
            {
                SetState(dispatchState.Clone());
                VisitPattern(arm.Pattern);
                SetState(StateWhenTrue);
                if (!reachableLabels.Contains(arm.Label) || arm.Pattern.HasErrors)
                {
                    SetUnreachable();
                }

                if (arm.WhenClause != null)
                {
                    VisitCondition(arm.WhenClause);
                    SetState(StateWhenTrue);
                }

                VisitRvalue(arm.Value);
                Join(ref endState, ref this.State);
            }

            SetState(endState);
            return node;
        }
    }
}
