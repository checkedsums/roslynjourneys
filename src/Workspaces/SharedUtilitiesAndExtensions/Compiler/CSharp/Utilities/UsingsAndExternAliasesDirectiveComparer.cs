// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Utilities;

internal class UsingsAndExternAliasesDirectiveComparer : IComparer<SyntaxNode?>
{
    public static readonly IComparer<SyntaxNode> NormalInstance = new UsingsAndExternAliasesDirectiveComparer(
        NameSyntaxComparer.Create(TokenComparer.NormalInstance),
        TokenComparer.NormalInstance);

    public static readonly IComparer<SyntaxNode> SystemFirstInstance = new UsingsAndExternAliasesDirectiveComparer(
        NameSyntaxComparer.Create(TokenComparer.SystemFirstInstance),
        TokenComparer.SystemFirstInstance);

    private readonly IComparer<NameSyntax> _nameComparer;
    private readonly IComparer<SyntaxToken> _tokenComparer;

    private UsingsAndExternAliasesDirectiveComparer(
        IComparer<NameSyntax> nameComparer,
        IComparer<SyntaxToken> tokenComparer)
    {
        RoslynDebug.AssertNotNull(nameComparer);
        RoslynDebug.AssertNotNull(tokenComparer);
        _nameComparer = nameComparer;
        _tokenComparer = tokenComparer;
    }

    private struct UsingData
    {
        internal bool isGlobal;
        internal UsingKind kind;
        public static implicit operator UsingData(UsingKind kind) => new() { kind = kind };
    }
    internal enum UsingKind : byte
    {
        Extern,
        Namespace,
        Alias
    }

    private static UsingData GetUsingKind(UsingDirectiveSyntax? usingDirective, ExternAliasDirectiveSyntax? externDirective)
    {
        if (externDirective != null)
        {
            return UsingKind.Extern;
        }
        else
        {
            RoslynDebug.AssertNotNull(usingDirective);
        }

        return new UsingData
        {
            kind = usingDirective.Alias is not null ? UsingKind.Alias : UsingKind.Namespace,
            isGlobal = usingDirective.GlobalKeyword != default,
        };
    }

    public int Compare(SyntaxNode? directive1, SyntaxNode? directive2)
    {
        if (directive1 is null)
            return directive2 is null ? 0 : -1;

        if (directive2 is null)
            return 1;

        if (directive1 == directive2)
            return 0;

        var using1 = directive1 as UsingDirectiveSyntax;
        var using2 = directive2 as UsingDirectiveSyntax;
        var extern1 = directive1 as ExternAliasDirectiveSyntax;
        var extern2 = directive2 as ExternAliasDirectiveSyntax;

        var directive1Kind = GetUsingKind(using1, extern1);
        var directive2Kind = GetUsingKind(using2, extern2);

        // different types of usings get broken up into groups.
        //  * externs
        //  * usings
        //  * using statics
        //  * aliases

        var directiveKindDifference = directive1Kind.kind - directive2Kind.kind;
        if (directiveKindDifference != 0)
            return directiveKindDifference;

        // Place global directives first.
        if (directive1Kind.isGlobal != directive2Kind.isGlobal)
            return directive1Kind.isGlobal ? -1 : 1;

        // ok, it's the same type of using now.
        switch (directive1Kind.kind)
        {
            case UsingKind.Extern:
                // they're externs, sort by the alias
                return _tokenComparer.Compare(extern1!.Identifier, extern2!.Identifier);

            case UsingKind.Alias:
                var aliasComparisonResult = _tokenComparer.Compare(using1!.Alias!.Name.Identifier, using2!.Alias!.Name.Identifier);

                if (aliasComparisonResult == 0 && using1.Name != null && using2.Name != null)
                {
                    // They both use the same alias, so compare the names.
                    return _nameComparer.Compare(using1.Name, using2.Name);
                }

                return aliasComparisonResult;

            default:
                Contract.ThrowIfNull(using1!.Name);
                Contract.ThrowIfNull(using2!.Name);
                return _nameComparer.Compare(using1!.Name, using2!.Name);
        }
    }
}
