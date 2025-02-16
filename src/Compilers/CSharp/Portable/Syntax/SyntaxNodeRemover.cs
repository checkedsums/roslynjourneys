﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class SyntaxNodeRemover
    {
        internal static TRoot? RemoveNodes<TRoot>(TRoot root, IEnumerable<SyntaxNode> nodes, SyntaxRemoveOptions options) where TRoot : SyntaxNode
        {
            if (nodes is null || nodes.ToArray().Length == 0)
                return root;

            var remover = new SyntaxRemover([.. nodes], options);
            var result = remover.Visit(root);

            var residualTrivia = remover.ResidualTrivia;

            // the result of the SyntaxRemover will be null when the root node is removed.
            if (result is not null && residualTrivia.Count > 0)
                result = result.WithTrailingTrivia(result.GetTrailingTrivia().Concat(residualTrivia));

            return (TRoot?)result;
        }

        private sealed class SyntaxRemover(SyntaxNode[] nodesToRemove, SyntaxRemoveOptions options) : CSharpSyntaxRewriter(nodesToRemove.Any(n => n.IsPartOfStructuredTrivia()))
        {
            private readonly HashSet<SyntaxNode> _nodesToRemove = [.. nodesToRemove];
            private readonly SyntaxRemoveOptions _options = options;
            private readonly TextSpan _searchSpan = ComputeTotalSpan(nodesToRemove);
            private readonly SyntaxTriviaListBuilder _residualTrivia = SyntaxTriviaListBuilder.Create();
            private HashSet<SyntaxNode>? _directivesToKeep;

            private static TextSpan ComputeTotalSpan(SyntaxNode[] nodes)
            {
                var span0 = nodes[0].FullSpan;
                int start = span0.Start;
                int end = span0.End;

                for (int i = 1; i < nodes.Length; i++)
                {
                    var span = nodes[i].FullSpan;
                    start = Math.Min(start, span.Start);
                    end = Math.Max(end, span.End);
                }

                return new TextSpan(start, end - start);
            }

            internal SyntaxTriviaList ResidualTrivia => _residualTrivia?.ToList() ?? default;

            private void AddResidualTrivia(SyntaxTriviaList trivia, bool requiresNewLine = false)
            {
                if (requiresNewLine)
                    this.AddEndOfLine(GetEndOfLine(trivia) ?? SyntaxFactory.CarriageReturnLineFeed);

                _residualTrivia.Add(trivia);
            }

            private void AddEndOfLine(SyntaxTrivia? eolTrivia)
            {
                if (!eolTrivia.HasValue)
                    return;

                if (_residualTrivia.Count == 0 || !IsEndOfLine(_residualTrivia[_residualTrivia.Count - 1]))
                    _residualTrivia.Add(eolTrivia.Value);
            }

            /// <summary>
            /// Returns whether the specified <see cref="SyntaxTrivia"/> token is also the end of the line.  This will
            /// be true for <see cref="SyntaxKind.EndOfLineTrivia"/>, <see cref="SyntaxKind.SingleLineCommentTrivia"/>,
            /// and all preprocessor directives.
            /// </summary>
            private static bool IsEndOfLine(SyntaxTrivia trivia)
            {
                return trivia.IsKind(SyntaxKind.EndOfLineTrivia) || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsDirective;
            }

            /// <summary>
            /// Returns the first end of line found in a <see cref="SyntaxTriviaList"/>.
            /// </summary>
            private static SyntaxTrivia? GetEndOfLine(SyntaxTriviaList list)
            {
                foreach (var trivia in list)
                {
                    if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                        return trivia;

                    if (trivia.IsDirective && trivia.GetStructure() is DirectiveTriviaSyntax directive)
                        return GetEndOfLine(directive.EndOfDirectiveToken.TrailingTrivia);
                }

                return null;
            }

            private bool IsForRemoval(SyntaxNode node) => _nodesToRemove.Contains(node);

            private bool ShouldVisit(SyntaxNode node) => node.FullSpan.IntersectsWith(_searchSpan) || (_residualTrivia != null && _residualTrivia.Count > 0);

            [return: NotNullIfNotNull(nameof(node))]
            public override SyntaxNode? Visit(SyntaxNode? node)
            {
                SyntaxNode? result = node;

                if (node != null)
                {
                    if (this.IsForRemoval(node))
                    {
                        this.AddTrivia(node);
                        result = null;
                    }
                    else if (this.ShouldVisit(node))
                    {
                        result = base.Visit(node);
                    }
                }

                return result;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                SyntaxToken result = token;

                // only bother visiting trivia if we are removing a node in structured trivia
                if (this.VisitIntoStructuredTrivia)
                {
                    result = base.VisitToken(token);
                }

                // the next token gets the accrued trivia.
                if (result.Kind() != SyntaxKind.None && _residualTrivia != null && _residualTrivia.Count > 0)
                {
                    _residualTrivia.Add(result.LeadingTrivia);
                    result = result.WithLeadingTrivia(_residualTrivia.ToList());
                    _residualTrivia.Clear();
                }

                return result;
            }

            // deal with separated lists and removal of associated separators
            public override SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list)
            {
                var withSeps = list.GetWithSeparators();
                bool removeNextSeparator = false;

                SyntaxNodeOrTokenListBuilder? alternate = null;
                for (int i = 0, n = withSeps.Count; i < n; i++)
                {
                    var item = withSeps[i];
                    SyntaxNodeOrToken visited;

                    if (item.IsToken) // separator
                    {
                        if (removeNextSeparator)
                        {
                            removeNextSeparator = false;
                            visited = default;
                        }
                        else
                            visited = this.VisitListSeparator(item.AsToken());
                    }
                    else
                    {
                        var node = (TNode)item.AsNode()!;

                        if (this.IsForRemoval(node))
                        {
                            if (alternate == null)
                            {
                                alternate = new SyntaxNodeOrTokenListBuilder(n);
                                alternate.Add(withSeps, 0, i);
                            }

                            CommonSyntaxNodeRemover.GetSeparatorInfo(withSeps, i, (int)SyntaxKind.EndOfLineTrivia, out bool nextTokenIsSeparator, out bool nextSeparatorBelongsToNode);

                            if (!nextSeparatorBelongsToNode &&
                                alternate.Count > 0 &&
                                alternate[^1].IsToken)
                            {
                                var separator = alternate[^1].AsToken();
                                this.AddTrivia(separator, node);
                                alternate.RemoveLast();
                            }
                            else if (nextTokenIsSeparator)
                            {
                                var separator = withSeps[i + 1].AsToken();
                                this.AddTrivia(node, separator);
                                removeNextSeparator = true;
                            }
                            else
                                this.AddTrivia(node);

                            visited = default;
                        }
                        else
                            visited = this.VisitListElement(node);
                    }

                    if (item != visited && alternate == null)
                    {
                        alternate = new SyntaxNodeOrTokenListBuilder(n);
                        alternate.Add(withSeps, 0, i);
                    }

                    if (alternate != null && visited.Kind() != SyntaxKind.None)
                        alternate.Add(visited);
                }

                return alternate?.ToList().AsSeparatedList<TNode>() ?? list;
            }

            private void AddTrivia(SyntaxNode node)
            {
                if ((_options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                    this.AddResidualTrivia(node.GetLeadingTrivia());
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0)
                    this.AddEndOfLine(GetEndOfLine(node.GetLeadingTrivia()));

                if ((_options & (SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepUnbalancedDirectives)) != 0)
                    this.AddDirectives(node, GetRemovedSpan(node.Span, node.FullSpan));

                if ((_options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                    this.AddResidualTrivia(node.GetTrailingTrivia());
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0)
                    this.AddEndOfLine(GetEndOfLine(node.GetTrailingTrivia()));

                if ((_options & SyntaxRemoveOptions.AddElasticMarker) != 0)
                    this.AddResidualTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));
            }

            private void AddTrivia(SyntaxToken token, SyntaxNode node)
            {
                Debug.Assert(node.Parent is not null);
                if ((_options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                {
                    this.AddResidualTrivia(token.LeadingTrivia);
                    this.AddResidualTrivia(token.TrailingTrivia);
                    this.AddResidualTrivia(node.GetLeadingTrivia());
                }
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0)
                {
                    // For retrieving an EOL we don't need to check the node leading trivia as
                    // it can be always retrieved from the token trailing trivia, if one exists.
                    var eol = GetEndOfLine(token.LeadingTrivia) ??
                              GetEndOfLine(token.TrailingTrivia);
                    this.AddEndOfLine(eol);
                }

                if ((_options & (SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepUnbalancedDirectives)) != 0)
                {
                    var span = TextSpan.FromBounds(token.Span.Start, node.Span.End);
                    var fullSpan = TextSpan.FromBounds(token.FullSpan.Start, node.FullSpan.End);
                    this.AddDirectives(node.Parent, GetRemovedSpan(span, fullSpan));
                }

                if ((_options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                    this.AddResidualTrivia(node.GetTrailingTrivia());
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0)
                    this.AddEndOfLine(GetEndOfLine(node.GetTrailingTrivia()));

                if ((_options & SyntaxRemoveOptions.AddElasticMarker) != 0)
                    this.AddResidualTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));
            }

            private void AddTrivia(SyntaxNode node, SyntaxToken token)
            {
                Debug.Assert(node.Parent is not null);
                if ((_options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                    this.AddResidualTrivia(node.GetLeadingTrivia());
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0)
                    this.AddEndOfLine(GetEndOfLine(node.GetLeadingTrivia()));

                if ((_options & (SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepUnbalancedDirectives)) != 0)
                {
                    var span = TextSpan.FromBounds(node.Span.Start, token.Span.End);
                    var fullSpan = TextSpan.FromBounds(node.FullSpan.Start, token.FullSpan.End);
                    this.AddDirectives(node.Parent, GetRemovedSpan(span, fullSpan));
                }

                if ((_options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                {
                    this.AddResidualTrivia(node.GetTrailingTrivia());
                    this.AddResidualTrivia(token.LeadingTrivia);
                    this.AddResidualTrivia(token.TrailingTrivia);
                }
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0)
                    // For retrieving an EOL we don't need to check the token leading trivia as
                    // it can be always retrieved from the node trailing trivia, if one exists.
                    this.AddEndOfLine(GetEndOfLine(node.GetTrailingTrivia()) ?? GetEndOfLine(token.TrailingTrivia));

                if ((_options & SyntaxRemoveOptions.AddElasticMarker) != 0)
                    this.AddResidualTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));
            }

            private TextSpan GetRemovedSpan(TextSpan span, TextSpan fullSpan)
            {
                var removedSpan = fullSpan;

                if ((_options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                    removedSpan = TextSpan.FromBounds(span.Start, removedSpan.End);

                if ((_options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                    removedSpan = TextSpan.FromBounds(removedSpan.Start, span.End);

                return removedSpan;
            }

            private void AddDirectives(SyntaxNode node, TextSpan span)
            {
                if (node.ContainsDirectives)
                {
                    (_directivesToKeep ??= []).Clear();

                    var directivesInSpan = node.DescendantTrivia(span, n => n.ContainsDirectives, descendIntoTrivia: true)
                                               .Where(tr => tr.IsDirective)
                                               .Select(tr => (DirectiveTriviaSyntax)tr.GetStructure()!);

                    foreach (var directive in directivesInSpan)
                    {
                        if ((_options & SyntaxRemoveOptions.KeepDirectives) != 0)
                            _directivesToKeep.Add(directive);
                        else if (directive.Kind() is SyntaxKind.DefineDirectiveTrivia or SyntaxKind.UndefDirectiveTrivia) // always keep #define and #undef, even if we are only keeping unbalanced directives
                            _directivesToKeep.Add(directive);
                        else if (HasRelatedDirectives(directive))
                        {
                            // a balanced directive with respect to a given node has all related directives rooted under that node
                            var relatedDirectives = directive.GetRelatedDirectives();
                            var balanced = relatedDirectives.All(rd => rd.FullSpan.OverlapsWith(span));

                            if (!balanced) // if not fully balanced, all related directives under the node are considered unbalanced.
                                foreach (var unbalancedDirective in relatedDirectives.Where(rd => rd.FullSpan.OverlapsWith(span)))
                                    _directivesToKeep.Add(unbalancedDirective);
                        }

                        if (_directivesToKeep.Contains(directive))
                        {
                            var parentTrivia = directive.ParentTrivia;
                            var (triviaList, directiveTriviaListIndex) = getTriviaListAndIndex(parentTrivia);

                            // If we're keeping a directive, and it's not at the start of the line, keep the indentation
                            // whitespace that precedes it as well so that the directive stays in the right location.
                            if (directiveTriviaListIndex >= 1 && triviaList[directiveTriviaListIndex - 1] is { RawKind: (int)SyntaxKind.WhitespaceTrivia } indentationTrivia)
                                AddResidualTrivia(SyntaxFactory.TriviaList(indentationTrivia, parentTrivia), requiresNewLine: true);
                            else
                                AddResidualTrivia(SyntaxFactory.TriviaList(parentTrivia), requiresNewLine: true);
                        }
                    }
                }

                static (SyntaxTriviaList triviaList, int index) getTriviaListAndIndex(SyntaxTrivia trivia)
                {
                    var parentToken = trivia.Token;

                    var index = parentToken.LeadingTrivia.IndexOf(trivia);
                    return index >= 0
                        ? (parentToken.LeadingTrivia, index)
                        : (parentToken.TrailingTrivia, parentToken.TrailingTrivia.IndexOf(trivia));
                }
            }

            private static bool HasRelatedDirectives(DirectiveTriviaSyntax directive)
                => directive.Kind() is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia or SyntaxKind.RegionDirectiveTrivia or SyntaxKind.EndRegionDirectiveTrivia;
        }
    }
}
