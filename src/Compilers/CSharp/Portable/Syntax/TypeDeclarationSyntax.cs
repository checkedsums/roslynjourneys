// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public abstract partial class TypeDeclarationSyntax
    {
        public int Arity
        {
            get
            {
                return this.TypeParameterList == null ? 0 : this.TypeParameterList.Parameters.Count;
            }
        }

        public new TypeDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items)
            => (TypeDeclarationSyntax)AddAttributeListsCore(items);

        public new TypeDeclarationSyntax AddModifiers(params SyntaxToken[] items)
            => (TypeDeclarationSyntax)AddModifiersCore(items);

        public new TypeDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists)
            => (TypeDeclarationSyntax)WithAttributeListsCore(attributeLists);

        public new TypeDeclarationSyntax WithModifiers(SyntaxTokenList modifiers)
            => (TypeDeclarationSyntax)WithModifiersCore(modifiers);

        internal PrimaryConstructorBaseTypeSyntax? PrimaryConstructorBaseTypeIfClass
        {
            get
            {
                if (Kind() is (SyntaxKind.RecordDeclaration or SyntaxKind.ClassDeclaration))
                {
                    return BaseList?.Types.FirstOrDefault() as PrimaryConstructorBaseTypeSyntax;
                }

                return null;
            }
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFactory
    {
        internal static SyntaxKind GetTypeDeclarationKeywordKind(DeclarationKind kind)
        {
            return kind switch
            {
                DeclarationKind.Class => SyntaxKind.ClassKeyword,
                DeclarationKind.Struct => SyntaxKind.StructKeyword,
                DeclarationKind.Interface => SyntaxKind.InterfaceKeyword,
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }

        private static SyntaxKind GetTypeDeclarationKeywordKind(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.ClassDeclaration => SyntaxKind.ClassKeyword,
                SyntaxKind.StructDeclaration => SyntaxKind.StructKeyword,
                SyntaxKind.InterfaceDeclaration => SyntaxKind.InterfaceKeyword,
                SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration => SyntaxKind.RecordKeyword,
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }

        public static TypeDeclarationSyntax TypeDeclaration(SyntaxKind kind, SyntaxToken identifier)
        {
            return TypeDeclaration(
                kind,
                default,
                default,
                SyntaxFactory.Token(GetTypeDeclarationKeywordKind(kind)),
                identifier,
                typeParameterList: null,
                baseList: null,
                default,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                default,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                default);
        }

        public static TypeDeclarationSyntax TypeDeclaration(SyntaxKind kind, string identifier)
        {
            return SyntaxFactory.TypeDeclaration(kind, SyntaxFactory.Identifier(identifier));
        }

        public static TypeDeclarationSyntax TypeDeclaration(
            SyntaxKind kind,
            SyntaxList<AttributeListSyntax> attributes,
            SyntaxTokenList modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax? typeParameterList,
            BaseListSyntax? baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
        {
            return kind switch
            {
                SyntaxKind.ClassDeclaration => SyntaxFactory.ClassDeclaration(attributes, modifiers, keyword, identifier, typeParameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken),
                SyntaxKind.StructDeclaration => SyntaxFactory.StructDeclaration(attributes, modifiers, keyword, identifier, typeParameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken),
                SyntaxKind.InterfaceDeclaration => SyntaxFactory.InterfaceDeclaration(attributes, modifiers, keyword, identifier, typeParameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken),
                SyntaxKind.RecordDeclaration => SyntaxFactory.RecordDeclaration(SyntaxKind.RecordDeclaration, attributes, modifiers, keyword, classOrStructKeyword: default, identifier, typeParameterList, parameterList: null, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken),
                SyntaxKind.RecordStructDeclaration => SyntaxFactory.RecordDeclaration(SyntaxKind.RecordStructDeclaration, attributes, modifiers, keyword, classOrStructKeyword: SyntaxFactory.Token(SyntaxKind.StructKeyword), identifier, typeParameterList, parameterList: null, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }
    }
}
