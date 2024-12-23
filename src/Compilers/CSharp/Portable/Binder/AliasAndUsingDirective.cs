// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct AliasAndUsingDirective(AliasSymbol alias, UsingDirectiveSyntax? usingDirective)
    {
        public readonly AliasSymbol Alias = alias;
        public readonly SyntaxReference? UsingDirectiveReference = usingDirective?.GetReference();

        public UsingDirectiveSyntax? UsingDirective => (UsingDirectiveSyntax?)UsingDirectiveReference?.GetSyntax();
    }
}
