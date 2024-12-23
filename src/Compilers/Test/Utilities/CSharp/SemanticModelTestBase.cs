// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class SemanticModelTestBase : CSharpTestBase
    {
        protected int GetPositionForBinding(SyntaxTree tree)
        {
            return GetSyntaxNodeForBinding(GetSyntaxNodeList(tree)).SpanStart;
        }

        protected int GetPositionForBinding(string code)
        {
            const string tag = "/*pos*/";

            return code.IndexOf(tag, StringComparison.Ordinal) + tag.Length;
        }

        protected List<ExpressionSyntax> GetExprSyntaxList(SyntaxTree syntaxTree)
        {
            return GetExprSyntaxList(syntaxTree.GetRoot(), null);
        }

        private List<ExpressionSyntax> GetExprSyntaxList(SyntaxNode node, List<ExpressionSyntax> exprSynList)
        {
            if (exprSynList == null)
                exprSynList = new List<ExpressionSyntax>();

            if (node is ExpressionSyntax)
            {
                exprSynList.Add(node as ExpressionSyntax);
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    exprSynList = GetExprSyntaxList(child.AsNode(), exprSynList);
            }

            return exprSynList;
        }

        protected ExpressionSyntax GetExprSyntaxForBinding(List<ExpressionSyntax> exprSynList, int index = 0)
        {
            var tagName = string.Format("bind{0}", index == 0 ? String.Empty : index.ToString());
            var startComment = string.Format("/*<{0}>*/", tagName);
            var endComment = string.Format("/*</{0}>*/", tagName);

            foreach (var exprSyntax in exprSynList)
            {
                string exprFullText = exprSyntax.ToFullString();
                exprFullText = exprFullText.Trim();

                if (exprFullText.StartsWith(startComment, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(endComment))
                        if (exprFullText.EndsWith(endComment, StringComparison.Ordinal))
                            return exprSyntax;
                        else
                            continue;
                    else
                        return exprSyntax;
                }

                if (exprFullText.EndsWith(endComment, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(startComment))
                        if (exprFullText.StartsWith(startComment, StringComparison.Ordinal))
                            return exprSyntax;
                        else
                            continue;
                    else
                        return exprSyntax;
                }
            }

            return null;
        }
    }
}
