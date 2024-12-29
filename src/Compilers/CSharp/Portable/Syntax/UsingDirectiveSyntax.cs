// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    /// <remarks> <para>This node is associated with the following syntax kinds:</para>
    /// <list type="bullet"> <item> <description> <see cref="SyntaxKind.UsingDirective"/> </description> </item> </list> </remarks>
    public partial class UsingDirectiveSyntax : CSharpSyntaxNode
    {
        private NameEqualsSyntax? alias;
        private TypeSyntax? namespaceOrType;

        internal UsingDirectiveSyntax(InternalSyntax.CSharpSyntaxNode green, SyntaxNode? parent, int position)
          : base(green, parent, position)
        {
        }

        public SyntaxToken GlobalKeyword
        {
            get
            {
                var slot = ((InternalSyntax.UsingDirectiveSyntax)this.Green).globalKeyword;
                return slot != null ? new SyntaxToken(this, slot, Position, 0) : default;
            }
        }

        public SyntaxToken UsingKeyword => new SyntaxToken(this, ((InternalSyntax.UsingDirectiveSyntax)this.Green).usingKeyword, GetChildPosition(1), GetChildIndex(1));

        public SyntaxToken UnsafeKeyword
        {
            get
            {
                var slot = ((Syntax.InternalSyntax.UsingDirectiveSyntax)this.Green).unsafeKeyword;
                return slot != null ? new SyntaxToken(this, slot, GetChildPosition(2), GetChildIndex(2)) : default;
            }
        }

        public NameEqualsSyntax? Alias => GetRed(ref this.alias, 3);

        public TypeSyntax NamespaceOrType => GetRed(ref this.namespaceOrType, 4)!;

        public SyntaxToken SemicolonToken => new SyntaxToken(this, ((InternalSyntax.UsingDirectiveSyntax)this.Green).semicolonToken, GetChildPosition(5), GetChildIndex(5));

        internal override SyntaxNode? GetNodeSlot(int index)
            => index switch
            {
                3 => GetRed(ref this.alias, 3),
                4 => GetRed(ref this.namespaceOrType, 4)!,
                _ => null,
            };

        internal override SyntaxNode? GetCachedSlot(int index)
            => index switch
            {
                3 => this.alias,
                4 => this.namespaceOrType,
                _ => null,
            };

        public override void Accept(CSharpSyntaxVisitor visitor) => visitor.VisitUsingDirective(this);
        public override TResult? Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor) where TResult : default => visitor.VisitUsingDirective(this);

        public UsingDirectiveSyntax Update(SyntaxToken globalKeyword, SyntaxToken usingKeyword, SyntaxToken unsafeKeyword, NameEqualsSyntax? alias, TypeSyntax namespaceOrType, SyntaxToken semicolonToken)
        {
            if (globalKeyword != this.GlobalKeyword || usingKeyword != this.UsingKeyword || unsafeKeyword != this.UnsafeKeyword || alias != this.Alias || namespaceOrType != this.NamespaceOrType || semicolonToken != this.SemicolonToken)
            {
                var newNode = SyntaxFactory.UsingDirective(globalKeyword, usingKeyword, unsafeKeyword, alias, namespaceOrType, semicolonToken);
                var annotations = GetAnnotations();
                return annotations?.Length > 0 ? newNode.WithAnnotations(annotations) : newNode;
            }

            return this;
        }

        public UsingDirectiveSyntax WithGlobalKeyword(SyntaxToken globalKeyword) => Update(globalKeyword, this.UsingKeyword, this.UnsafeKeyword, this.Alias, this.NamespaceOrType, this.SemicolonToken);
        public UsingDirectiveSyntax WithUsingKeyword(SyntaxToken usingKeyword) => Update(this.GlobalKeyword, usingKeyword, this.UnsafeKeyword, this.Alias, this.NamespaceOrType, this.SemicolonToken);
        public UsingDirectiveSyntax WithUnsafeKeyword(SyntaxToken unsafeKeyword) => Update(this.GlobalKeyword, this.UsingKeyword, unsafeKeyword, this.Alias, this.NamespaceOrType, this.SemicolonToken);
        public UsingDirectiveSyntax WithAlias(NameEqualsSyntax? alias) => Update(this.GlobalKeyword, this.UsingKeyword, this.UnsafeKeyword, alias, this.NamespaceOrType, this.SemicolonToken);
        public UsingDirectiveSyntax WithNamespaceOrType(TypeSyntax namespaceOrType) => Update(this.GlobalKeyword, this.UsingKeyword, this.UnsafeKeyword, this.Alias, namespaceOrType, this.SemicolonToken);
        public UsingDirectiveSyntax WithSemicolonToken(SyntaxToken semicolonToken) => Update(this.GlobalKeyword, this.UsingKeyword, this.UnsafeKeyword, this.Alias, this.NamespaceOrType, semicolonToken);

        /// <summary>
        /// Returns the name this <see cref="UsingDirectiveSyntax"/> points at, or <see langword="null"/> if it does not
        /// point at a name.  A normal <c>using X.Y.Z;</c> or <c>using static X.Y.Z;</c> will always point at a name and
        /// will always return a value for this.  However, a using-alias (e.g. <c>using x = ...;</c>) may or may not
        /// point at a name and may return <see langword="null"/> here.  An example of when that may happen is the type
        /// on the right side of the <c>=</c> is not a name.  For example <c>using x = (X.Y.Z, A.B.C);</c>.  Here, as
        /// the type is a tuple-type there is no name to return.
        /// </summary>
        public NameSyntax? Name => this.NamespaceOrType as NameSyntax;

        public UsingDirectiveSyntax Update(SyntaxToken usingKeyword, NameEqualsSyntax? alias, NameSyntax name, SyntaxToken semicolonToken)
            => this.Update(this.GlobalKeyword, usingKeyword, this.UnsafeKeyword, alias, namespaceOrType: name, semicolonToken);

        public UsingDirectiveSyntax Update(SyntaxToken globalKeyword, SyntaxToken usingKeyword, NameEqualsSyntax? alias, NameSyntax name, SyntaxToken semicolonToken)
            => this.Update(globalKeyword, usingKeyword, this.UnsafeKeyword, alias, namespaceOrType: name, semicolonToken);

        public UsingDirectiveSyntax WithName(NameSyntax name)
            => WithNamespaceOrType(name);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new UsingDirectiveSyntax instance.</summary>
        public static UsingDirectiveSyntax UsingDirective(TypeSyntax namespaceOrType)
            => UsingDirective(default, Token(SyntaxKind.UsingKeyword), default, null, namespaceOrType, Token(SyntaxKind.SemicolonToken));

        /// <summary>Creates a new UsingDirectiveSyntax instance.</summary>
        public static UsingDirectiveSyntax UsingDirective(NameEqualsSyntax alias, NameSyntax name)
        {
            return UsingDirective(
                usingKeyword: Token(SyntaxKind.UsingKeyword),
                alias: alias,
                name: name,
                semicolonToken: Token(SyntaxKind.SemicolonToken));
        }

        public static UsingDirectiveSyntax UsingDirective(SyntaxToken usingKeyword, NameEqualsSyntax? alias, NameSyntax name, SyntaxToken semicolonToken)
        {
            return UsingDirective(
                globalKeyword: default,
                usingKeyword,
                unsafeKeyword: default,
                alias,
                namespaceOrType: name,
                semicolonToken);
        }

        /// <summary>Creates a new UsingDirectiveSyntax instance.</summary>
        public static UsingDirectiveSyntax UsingDirective(NameSyntax name)
            => UsingDirective(namespaceOrType: name);

        /// <summary>Creates a new UsingDirectiveSyntax instance.</summary>
        public static UsingDirectiveSyntax UsingDirective(SyntaxToken globalKeyword, SyntaxToken usingKeyword, SyntaxToken unsafeKeyword, NameEqualsSyntax? alias, TypeSyntax namespaceOrType, SyntaxToken semicolonToken)
        {
            switch (globalKeyword.Kind())
            {
                case SyntaxKind.GlobalKeyword:
                case SyntaxKind.None: break;
                default: throw new ArgumentException(nameof(globalKeyword));
            }
            if (usingKeyword.Kind() != SyntaxKind.UsingKeyword) throw new ArgumentException(nameof(usingKeyword));
            switch (unsafeKeyword.Kind())
            {
                case SyntaxKind.UnsafeKeyword:
                case SyntaxKind.None: break;
                default: throw new ArgumentException(nameof(unsafeKeyword));
            }
            if (namespaceOrType == null) throw new ArgumentNullException(nameof(namespaceOrType));
            if (semicolonToken.Kind() != SyntaxKind.SemicolonToken) throw new ArgumentException(nameof(semicolonToken));
            return (UsingDirectiveSyntax)Syntax.InternalSyntax.SyntaxFactory.UsingDirective((Syntax.InternalSyntax.SyntaxToken?)globalKeyword.Node, (Syntax.InternalSyntax.SyntaxToken)usingKeyword.Node!, (Syntax.InternalSyntax.SyntaxToken?)unsafeKeyword.Node, alias == null ? null : (Syntax.InternalSyntax.NameEqualsSyntax)alias.Green, (Syntax.InternalSyntax.TypeSyntax)namespaceOrType.Green, (Syntax.InternalSyntax.SyntaxToken)semicolonToken.Node!).CreateRed();
        }
    }
}
