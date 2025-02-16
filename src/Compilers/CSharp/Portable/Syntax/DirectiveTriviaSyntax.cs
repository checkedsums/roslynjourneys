﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class DirectiveTriviaSyntax
    {
        public SyntaxToken DirectiveNameToken
        {
            get
            {
                return this.Kind() switch
                {
                    SyntaxKind.IfDirectiveTrivia => ((IfDirectiveTriviaSyntax)this).IfKeyword,
                    SyntaxKind.ElifDirectiveTrivia => ((ElifDirectiveTriviaSyntax)this).ElifKeyword,
                    SyntaxKind.ElseDirectiveTrivia => ((ElseDirectiveTriviaSyntax)this).ElseKeyword,
                    SyntaxKind.EndIfDirectiveTrivia => ((EndIfDirectiveTriviaSyntax)this).EndIfKeyword,
                    SyntaxKind.RegionDirectiveTrivia => ((RegionDirectiveTriviaSyntax)this).RegionKeyword,
                    SyntaxKind.EndRegionDirectiveTrivia => ((EndRegionDirectiveTriviaSyntax)this).EndRegionKeyword,
                    SyntaxKind.ErrorDirectiveTrivia => ((ErrorDirectiveTriviaSyntax)this).ErrorKeyword,
                    SyntaxKind.WarningDirectiveTrivia => ((WarningDirectiveTriviaSyntax)this).WarningKeyword,
                    SyntaxKind.BadDirectiveTrivia => ((BadDirectiveTriviaSyntax)this).Identifier,
                    SyntaxKind.DefineDirectiveTrivia => ((DefineDirectiveTriviaSyntax)this).DefineKeyword,
                    SyntaxKind.UndefDirectiveTrivia => ((UndefDirectiveTriviaSyntax)this).UndefKeyword,
                    SyntaxKind.LineDirectiveTrivia => ((LineDirectiveTriviaSyntax)this).LineKeyword,
                    SyntaxKind.LineSpanDirectiveTrivia => ((LineSpanDirectiveTriviaSyntax)this).LineKeyword,
                    SyntaxKind.PragmaWarningDirectiveTrivia => ((PragmaWarningDirectiveTriviaSyntax)this).PragmaKeyword,
                    SyntaxKind.PragmaChecksumDirectiveTrivia => ((PragmaChecksumDirectiveTriviaSyntax)this).PragmaKeyword,
                    SyntaxKind.ReferenceDirectiveTrivia => ((ReferenceDirectiveTriviaSyntax)this).ReferenceKeyword,
                    SyntaxKind.LoadDirectiveTrivia => ((LoadDirectiveTriviaSyntax)this).LoadKeyword,
                    SyntaxKind.ShebangDirectiveTrivia => ((ShebangDirectiveTriviaSyntax)this).ExclamationToken,
                    _ => throw ExceptionUtilities.UnexpectedValue(this.Kind()),
                };
            }
        }

        public DirectiveTriviaSyntax? GetNextDirective(Func<DirectiveTriviaSyntax, bool>? predicate = null)
        {
            var token = (SyntaxToken)this.ParentTrivia.Token;
            bool next = false;
            while (token.Kind() != SyntaxKind.None)
            {
                foreach (var tr in token.LeadingTrivia)
                {
                    if (tr.IsDirective)
                    {
                        var d = (DirectiveTriviaSyntax)tr.GetStructure()!;
                        if (next)
                        {
                            if (predicate == null || predicate(d))
                            {
                                return d;
                            }
                        }
                        else if (tr.UnderlyingNode == this.Green && tr.SpanStart == this.SpanStart && (object)d == this)
                        {
                            next = true;
                        }
                    }
                }

                token = token.GetNextToken(s_hasDirectivesFunction);
            }

            return null;
        }

        public DirectiveTriviaSyntax? GetPreviousDirective(Func<DirectiveTriviaSyntax, bool>? predicate = null)
        {
            var token = (SyntaxToken)this.ParentTrivia.Token;
            bool next = false;
            while (token.Kind() != SyntaxKind.None)
            {
                foreach (var tr in token.LeadingTrivia.Reverse())
                {
                    if (tr.IsDirective)
                    {
                        var d = (DirectiveTriviaSyntax)tr.GetStructure()!;
                        if (next)
                        {
                            if (predicate == null || predicate(d))
                            {
                                return d;
                            }
                        }
                        else if (tr.UnderlyingNode == this.Green && tr.SpanStart == this.SpanStart && (object)d == this)
                        {
                            next = true;
                        }
                    }
                }

                token = token.GetPreviousToken(s_hasDirectivesFunction);
            }

            return null;
        }

        public List<DirectiveTriviaSyntax> GetRelatedDirectives()
        {
            var list = new List<DirectiveTriviaSyntax>();
            this.GetRelatedDirectives(list);
            return list;
        }

        private void GetRelatedDirectives(List<DirectiveTriviaSyntax> list)
        {
            list.Clear();
            var p = this.GetPreviousRelatedDirective();
            while (p != null)
            {
                list.Add(p);
                p = p.GetPreviousRelatedDirective();
            }

            list.Reverse();
            list.Add(this);
            var n = this.GetNextRelatedDirective();
            while (n != null)
            {
                list.Add(n);
                n = n.GetNextRelatedDirective();
            }
        }

        private DirectiveTriviaSyntax? GetNextRelatedDirective()
        {
            DirectiveTriviaSyntax? d = this;
            switch (d.Kind())
            {
                case SyntaxKind.IfDirectiveTrivia:
                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.ElifDirectiveTrivia:
                            case SyntaxKind.ElseDirectiveTrivia:
                            case SyntaxKind.EndIfDirectiveTrivia:
                                return d;
                        }

                        d = d.GetNextPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.ElifDirectiveTrivia:
                    d = d.GetNextPossiblyRelatedDirective();

                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.ElifDirectiveTrivia:
                            case SyntaxKind.ElseDirectiveTrivia:
                            case SyntaxKind.EndIfDirectiveTrivia:
                                return d;
                        }

                        d = d.GetNextPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.ElseDirectiveTrivia:
                    while (d != null)
                    {
                        if (d.Kind() == SyntaxKind.EndIfDirectiveTrivia)
                        {
                            return d;
                        }

                        d = d.GetNextPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.RegionDirectiveTrivia:
                    while (d != null)
                    {
                        if (d.Kind() == SyntaxKind.EndRegionDirectiveTrivia)
                        {
                            return d;
                        }

                        d = d.GetNextPossiblyRelatedDirective();
                    }

                    break;
            }

            return null;
        }

        private DirectiveTriviaSyntax? GetNextPossiblyRelatedDirective()
        {
            DirectiveTriviaSyntax? d = this;
            while (d != null)
            {
                d = d.GetNextDirective();
                if (d != null)
                {
                    // skip matched sets
                    switch (d.Kind())
                    {
                        case SyntaxKind.IfDirectiveTrivia:
                            while (d != null && d.Kind() != SyntaxKind.EndIfDirectiveTrivia)
                            {
                                d = d.GetNextRelatedDirective();
                            }

                            continue;
                        case SyntaxKind.RegionDirectiveTrivia:
                            while (d != null && d.Kind() != SyntaxKind.EndRegionDirectiveTrivia)
                            {
                                d = d.GetNextRelatedDirective();
                            }

                            continue;
                    }
                }

                return d;
            }

            return null;
        }

        private DirectiveTriviaSyntax? GetPreviousRelatedDirective()
        {
            DirectiveTriviaSyntax? d = this;
            switch (d.Kind())
            {
                case SyntaxKind.EndIfDirectiveTrivia:
                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.IfDirectiveTrivia:
                            case SyntaxKind.ElifDirectiveTrivia:
                            case SyntaxKind.ElseDirectiveTrivia:
                                return d;
                        }

                        d = d.GetPreviousPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.ElifDirectiveTrivia:
                    d = d.GetPreviousPossiblyRelatedDirective();

                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.IfDirectiveTrivia:
                            case SyntaxKind.ElifDirectiveTrivia:
                                return d;
                        }

                        d = d.GetPreviousPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.ElseDirectiveTrivia:
                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.IfDirectiveTrivia:
                            case SyntaxKind.ElifDirectiveTrivia:
                                return d;
                        }

                        d = d.GetPreviousPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.EndRegionDirectiveTrivia:
                    while (d != null)
                    {
                        if (d.Kind() == SyntaxKind.RegionDirectiveTrivia)
                        {
                            return d;
                        }

                        d = d.GetPreviousPossiblyRelatedDirective();
                    }

                    break;
            }

            return null;
        }

        private DirectiveTriviaSyntax? GetPreviousPossiblyRelatedDirective()
        {
            DirectiveTriviaSyntax? d = this;
            while (d != null)
            {
                d = d.GetPreviousDirective();
                if (d != null)
                {
                    // skip matched sets
                    switch (d.Kind())
                    {
                        case SyntaxKind.EndIfDirectiveTrivia:
                            while (d != null && d.Kind() != SyntaxKind.IfDirectiveTrivia)
                            {
                                d = d.GetPreviousRelatedDirective();
                            }

                            continue;
                        case SyntaxKind.EndRegionDirectiveTrivia:
                            while (d != null && d.Kind() != SyntaxKind.RegionDirectiveTrivia)
                            {
                                d = d.GetPreviousRelatedDirective();
                            }

                            continue;
                    }
                }

                return d;
            }

            return null;
        }

        private static readonly Func<SyntaxToken, bool> s_hasDirectivesFunction = t => t.ContainsDirectives;
    }
}
