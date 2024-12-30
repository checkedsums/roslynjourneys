// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFacts
    {
        public static bool IsKeywordKind(SyntaxKind kind) => IsReservedKeyword(kind) || IsContextualKeyword(kind);

        public static IEnumerable<SyntaxKind> ReservedKeywordKinds
        {
            get
            {
                for (int i = (int)SyntaxKind.BoolKeyword; i <= (int)SyntaxKind.ImplicitKeyword; i++)
                {
                    Debug.Assert(Enum.IsDefined(typeof(SyntaxKind), (SyntaxKind)i));
                    yield return (SyntaxKind)i;
                }
            }
        }

        public static IEnumerable<SyntaxKind> GetKeywordKinds()
        {
            foreach (var reserved in ReservedKeywordKinds)
                yield return reserved;

            foreach (var contextual in GetContextualKeywordKinds())
                yield return contextual;
        }

        public static bool IsReservedKeyword(SyntaxKind kind)
            => kind is >= SyntaxKind.BoolKeyword and <= SyntaxKind.ImplicitKeyword;

        public static bool IsAttributeTargetSpecifier(SyntaxKind kind)
            => kind is SyntaxKind.AssemblyKeyword or SyntaxKind.ModuleKeyword or SyntaxKind.EventKeyword or SyntaxKind.FieldKeyword or SyntaxKind.MethodKeyword or SyntaxKind.ParamKeyword or SyntaxKind.PropertyKeyword or SyntaxKind.ReturnKeyword or SyntaxKind.TypeKeyword or SyntaxKind.TypeVarKeyword;

        public static bool IsAccessibilityModifier(SyntaxKind kind)
            => kind is SyntaxKind.PrivateKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword or SyntaxKind.PublicKeyword;

        public static bool IsPreprocessorKeyword(SyntaxKind kind)
            => kind is SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword or SyntaxKind.DefaultKeyword or SyntaxKind.IfKeyword or SyntaxKind.ElseKeyword or SyntaxKind.ElifKeyword or SyntaxKind.EndIfKeyword or SyntaxKind.RegionKeyword or SyntaxKind.EndRegionKeyword or SyntaxKind.DefineKeyword or SyntaxKind.UndefKeyword or SyntaxKind.WarningKeyword or SyntaxKind.ErrorKeyword or SyntaxKind.LineKeyword or SyntaxKind.PragmaKeyword or SyntaxKind.HiddenKeyword or SyntaxKind.ChecksumKeyword or SyntaxKind.DisableKeyword or SyntaxKind.RestoreKeyword or SyntaxKind.ReferenceKeyword or SyntaxKind.LoadKeyword or SyntaxKind.ContextKeyword or SyntaxKind.EnableKeyword or SyntaxKind.WarningsKeyword or SyntaxKind.AnnotationsKeyword;

        /// <summary>
        /// Some preprocessor keywords are only keywords when they appear after a
        /// hash sign (#).  For these keywords, the lexer will produce tokens with
        /// Kind = SyntaxKind.IdentifierToken and ContextualKind set to the keyword
        /// SyntaxKind.
        /// </summary>
        /// <remarks>
        /// This wrinkle is specifically not publicly exposed.
        /// </remarks>
        internal static bool IsPreprocessorContextualKeyword(SyntaxKind kind)
            => kind is SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword or SyntaxKind.DefaultKeyword or SyntaxKind.HiddenKeyword or SyntaxKind.ChecksumKeyword or SyntaxKind.DisableKeyword or SyntaxKind.RestoreKeyword or SyntaxKind.EnableKeyword or SyntaxKind.WarningsKeyword or SyntaxKind.AnnotationsKeyword
            || IsPreprocessorKeyword(kind);

        public static bool IsPunctuation(SyntaxKind kind)
            => kind is >= SyntaxKind.TildeToken and <= SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken;

        public static bool IsLanguagePunctuation(SyntaxKind kind)
            => IsPunctuation(kind) && !IsPreprocessorKeyword(kind) && !IsDebuggerSpecialPunctuation(kind);

        private static bool IsDebuggerSpecialPunctuation(SyntaxKind kind) // TODO: What about "<>f_AnonymousMethod"? Or "123#"? What's this used for?
            => kind == SyntaxKind.DollarToken;

        public static bool IsPunctuationOrKeyword(SyntaxKind kind)
            => kind is >= SyntaxKind.TildeToken and <= SyntaxKind.EndOfFileToken;

        internal static bool IsLiteral(SyntaxKind kind)
            => kind is SyntaxKind.IdentifierToken or SyntaxKind.StringLiteralToken or SyntaxKind.Utf8StringLiteralToken or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken or SyntaxKind.CharacterLiteralToken or SyntaxKind.NumericLiteralToken or SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlTextLiteralNewLineToken or SyntaxKind.XmlEntityLiteralToken;

        public static bool IsAnyToken(SyntaxKind kind)
            => kind is >= SyntaxKind.TildeToken and < SyntaxKind.EndOfLineTrivia
            || kind is SyntaxKind.InterpolatedStringToken or SyntaxKind.InterpolatedStringStartToken or SyntaxKind.InterpolatedVerbatimStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken or SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedStringTextToken or SyntaxKind.InterpolatedStringEndToken or SyntaxKind.InterpolatedRawStringEndToken or SyntaxKind.LoadKeyword or SyntaxKind.ContextKeyword or SyntaxKind.EnableKeyword or SyntaxKind.UnderscoreToken or SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.SingleLineRawStringLiteralToken;

        public static bool IsPreprocessorDirective(SyntaxKind kind)
            => kind is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia or SyntaxKind.RegionDirectiveTrivia or SyntaxKind.EndRegionDirectiveTrivia or SyntaxKind.DefineDirectiveTrivia or SyntaxKind.UndefDirectiveTrivia or SyntaxKind.ErrorDirectiveTrivia or SyntaxKind.WarningDirectiveTrivia or SyntaxKind.LineDirectiveTrivia or SyntaxKind.LineSpanDirectiveTrivia or SyntaxKind.PragmaWarningDirectiveTrivia or SyntaxKind.PragmaChecksumDirectiveTrivia or SyntaxKind.ReferenceDirectiveTrivia or SyntaxKind.LoadDirectiveTrivia or SyntaxKind.BadDirectiveTrivia or SyntaxKind.ShebangDirectiveTrivia;

        public static bool IsName(SyntaxKind kind)
            => kind is SyntaxKind.IdentifierName or SyntaxKind.GenericName or SyntaxKind.QualifiedName or SyntaxKind.AliasQualifiedName;

        public static bool IsPredefinedType(SyntaxKind kind)
            => kind is SyntaxKind.BoolKeyword or SyntaxKind.ByteKeyword or SyntaxKind.SByteKeyword or SyntaxKind.IntKeyword or SyntaxKind.UIntKeyword or SyntaxKind.ShortKeyword or SyntaxKind.UShortKeyword or SyntaxKind.LongKeyword or SyntaxKind.ULongKeyword or SyntaxKind.FloatKeyword or SyntaxKind.DoubleKeyword or SyntaxKind.DecimalKeyword or SyntaxKind.StringKeyword or SyntaxKind.CharKeyword or SyntaxKind.ObjectKeyword or SyntaxKind.VoidKeyword;

        public static bool IsTypeSyntax(SyntaxKind kind)
            => kind is SyntaxKind.ArrayType or SyntaxKind.PointerType or SyntaxKind.NullableType or SyntaxKind.PredefinedType or SyntaxKind.TupleType or SyntaxKind.FunctionPointerType;

        /// <summary>
        /// Member declarations that can appear in global code (other than type declarations).
        /// </summary>
        public static bool IsGlobalMemberDeclaration(SyntaxKind kind)
            => kind is SyntaxKind.GlobalStatement or SyntaxKind.FieldDeclaration or SyntaxKind.MethodDeclaration or SyntaxKind.PropertyDeclaration or SyntaxKind.EventDeclaration or SyntaxKind.EventFieldDeclaration;

        public static bool IsTypeDeclaration(SyntaxKind kind)
            => kind is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration or SyntaxKind.DelegateDeclaration or SyntaxKind.EnumDeclaration or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration;

        public static bool IsNamespaceMemberDeclaration(SyntaxKind kind)
            => IsTypeDeclaration(kind) || kind is SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration;

        public static bool IsAnyUnaryExpression(SyntaxKind token) => IsPrefixUnaryExpression(token) || IsPostfixUnaryExpression(token);

        public static bool IsPrefixUnaryExpression(SyntaxKind token) => GetPrefixUnaryExpression(token) != SyntaxKind.None;

        public static bool IsPrefixUnaryExpressionOperatorToken(SyntaxKind token) => GetPrefixUnaryExpression(token) != SyntaxKind.None;

        public static SyntaxKind GetPrefixUnaryExpression(SyntaxKind token) => token switch
        {
            SyntaxKind.PlusToken => SyntaxKind.UnaryPlusExpression,
            SyntaxKind.MinusToken => SyntaxKind.UnaryMinusExpression,
            SyntaxKind.TildeToken => SyntaxKind.BitwiseNotExpression,
            SyntaxKind.ExclamationToken => SyntaxKind.LogicalNotExpression,
            SyntaxKind.PlusPlusToken => SyntaxKind.PreIncrementExpression,
            SyntaxKind.MinusMinusToken => SyntaxKind.PreDecrementExpression,
            SyntaxKind.AmpersandToken => SyntaxKind.AddressOfExpression,
            SyntaxKind.AsteriskToken => SyntaxKind.PointerIndirectionExpression,
            SyntaxKind.CaretToken => SyntaxKind.IndexExpression,
            _ => SyntaxKind.None,
        };

        public static bool IsPostfixUnaryExpression(SyntaxKind token) => GetPostfixUnaryExpression(token) != SyntaxKind.None;

        public static SyntaxKind GetPostfixUnaryExpression(SyntaxKind token) => token switch
        {
            SyntaxKind.PlusPlusToken => SyntaxKind.PostIncrementExpression,
            SyntaxKind.MinusMinusToken => SyntaxKind.PostDecrementExpression,
            SyntaxKind.ExclamationToken => SyntaxKind.SuppressNullableWarningExpression,
            _ => SyntaxKind.None,
        };

        public static bool IsUnaryOperatorDeclarationToken(SyntaxKind token)
        {
            return IsPrefixUnaryExpressionOperatorToken(token) ||
                   token == SyntaxKind.TrueKeyword ||
                   token == SyntaxKind.FalseKeyword;
        }

        public static bool IsAnyOverloadableOperator(SyntaxKind kind)
        {
            return IsOverloadableBinaryOperator(kind) || IsOverloadableUnaryOperator(kind);
        }

        public static bool IsOverloadableBinaryOperator(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.AsteriskToken or SyntaxKind.SlashToken or SyntaxKind.PercentToken or SyntaxKind.CaretToken or SyntaxKind.AmpersandToken or SyntaxKind.BarToken or SyntaxKind.EqualsEqualsToken or SyntaxKind.LessThanToken or SyntaxKind.LessThanEqualsToken or SyntaxKind.LessThanLessThanToken or SyntaxKind.GreaterThanToken or SyntaxKind.GreaterThanEqualsToken or SyntaxKind.GreaterThanGreaterThanToken or SyntaxKind.GreaterThanGreaterThanGreaterThanToken or SyntaxKind.ExclamationEqualsToken => true,
                _ => false,
            };
        }

        public static bool IsOverloadableUnaryOperator(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.TildeToken or SyntaxKind.ExclamationToken or SyntaxKind.PlusPlusToken or SyntaxKind.MinusMinusToken or SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword => true,
                _ => false,
            };
        }

        public static bool IsPrimaryFunction(SyntaxKind keyword)
        {
            return GetPrimaryFunction(keyword) != SyntaxKind.None;
        }

        public static SyntaxKind GetPrimaryFunction(SyntaxKind keyword)
        {
            return keyword switch
            {
                SyntaxKind.MakeRefKeyword => SyntaxKind.MakeRefExpression,
                SyntaxKind.RefTypeKeyword => SyntaxKind.RefTypeExpression,
                SyntaxKind.RefValueKeyword => SyntaxKind.RefValueExpression,
                SyntaxKind.CheckedKeyword => SyntaxKind.CheckedExpression,
                SyntaxKind.UncheckedKeyword => SyntaxKind.UncheckedExpression,
                SyntaxKind.DefaultKeyword => SyntaxKind.DefaultExpression,
                SyntaxKind.TypeOfKeyword => SyntaxKind.TypeOfExpression,
                SyntaxKind.SizeOfKeyword => SyntaxKind.SizeOfExpression,
                _ => SyntaxKind.None,
            };
        }

        public static bool IsLiteralExpression(SyntaxKind token)
        {
            return GetLiteralExpression(token) != SyntaxKind.None;
        }

        public static SyntaxKind GetLiteralExpression(SyntaxKind token)
        {
            return token switch
            {
                SyntaxKind.StringLiteralToken => SyntaxKind.StringLiteralExpression,
                SyntaxKind.Utf8StringLiteralToken => SyntaxKind.Utf8StringLiteralExpression,
                SyntaxKind.SingleLineRawStringLiteralToken => SyntaxKind.StringLiteralExpression,
                SyntaxKind.Utf8SingleLineRawStringLiteralToken => SyntaxKind.Utf8StringLiteralExpression,
                SyntaxKind.MultiLineRawStringLiteralToken => SyntaxKind.StringLiteralExpression,
                SyntaxKind.Utf8MultiLineRawStringLiteralToken => SyntaxKind.Utf8StringLiteralExpression,
                SyntaxKind.CharacterLiteralToken => SyntaxKind.CharacterLiteralExpression,
                SyntaxKind.NumericLiteralToken => SyntaxKind.NumericLiteralExpression,
                SyntaxKind.NullKeyword or SyntaxKind.VoidKeyword => SyntaxKind.NullLiteralExpression,
                SyntaxKind.TrueKeyword => SyntaxKind.TrueLiteralExpression,
                SyntaxKind.FalseKeyword => SyntaxKind.FalseLiteralExpression,
                SyntaxKind.ArgListKeyword => SyntaxKind.ArgListExpression,
                _ => SyntaxKind.None,
            };
        }

        public static bool IsInstanceExpression(SyntaxKind token)
        {
            return GetInstanceExpression(token) != SyntaxKind.None;
        }

        public static SyntaxKind GetInstanceExpression(SyntaxKind token)
        {
            return token switch
            {
                SyntaxKind.ThisKeyword => SyntaxKind.ThisExpression,
                SyntaxKind.BaseKeyword => SyntaxKind.BaseExpression,
                _ => SyntaxKind.None,
            };
        }

        public static bool IsBinaryExpression(SyntaxKind token)
        {
            return GetBinaryExpression(token) != SyntaxKind.None;
        }

        public static bool IsBinaryExpressionOperatorToken(SyntaxKind token)
        {
            return GetBinaryExpression(token) != SyntaxKind.None;
        }

        public static SyntaxKind GetBinaryExpression(SyntaxKind token)
        {
            return token switch
            {
                SyntaxKind.QuestionQuestionToken => SyntaxKind.CoalesceExpression,
                SyntaxKind.IsKeyword => SyntaxKind.IsExpression,
                SyntaxKind.AsKeyword => SyntaxKind.AsExpression,
                SyntaxKind.BarToken => SyntaxKind.BitwiseOrExpression,
                SyntaxKind.CaretToken => SyntaxKind.ExclusiveOrExpression,
                SyntaxKind.AmpersandToken => SyntaxKind.BitwiseAndExpression,
                SyntaxKind.EqualsEqualsToken => SyntaxKind.EqualsExpression,
                SyntaxKind.ExclamationEqualsToken => SyntaxKind.NotEqualsExpression,
                SyntaxKind.LessThanToken => SyntaxKind.LessThanExpression,
                SyntaxKind.LessThanEqualsToken => SyntaxKind.LessThanOrEqualExpression,
                SyntaxKind.GreaterThanToken => SyntaxKind.GreaterThanExpression,
                SyntaxKind.GreaterThanEqualsToken => SyntaxKind.GreaterThanOrEqualExpression,
                SyntaxKind.LessThanLessThanToken => SyntaxKind.LeftShiftExpression,
                SyntaxKind.GreaterThanGreaterThanToken => SyntaxKind.RightShiftExpression,
                SyntaxKind.GreaterThanGreaterThanGreaterThanToken => SyntaxKind.UnsignedRightShiftExpression,
                SyntaxKind.PlusToken => SyntaxKind.AddExpression,
                SyntaxKind.MinusToken => SyntaxKind.SubtractExpression,
                SyntaxKind.AsteriskToken => SyntaxKind.MultiplyExpression,
                SyntaxKind.SlashToken => SyntaxKind.DivideExpression,
                SyntaxKind.PercentToken => SyntaxKind.ModuloExpression,
                SyntaxKind.AmpersandAmpersandToken => SyntaxKind.LogicalAndExpression,
                SyntaxKind.BarBarToken => SyntaxKind.LogicalOrExpression,
                _ => SyntaxKind.None,
            };
        }

        public static bool IsAssignmentExpression(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.CoalesceAssignmentExpression or SyntaxKind.OrAssignmentExpression or SyntaxKind.AndAssignmentExpression or SyntaxKind.ExclusiveOrAssignmentExpression or SyntaxKind.LeftShiftAssignmentExpression or SyntaxKind.RightShiftAssignmentExpression or SyntaxKind.UnsignedRightShiftAssignmentExpression or SyntaxKind.AddAssignmentExpression or SyntaxKind.SubtractAssignmentExpression or SyntaxKind.MultiplyAssignmentExpression or SyntaxKind.DivideAssignmentExpression or SyntaxKind.ModuloAssignmentExpression or SyntaxKind.SimpleAssignmentExpression => true,
                _ => false,
            };
        }

        public static bool IsAssignmentExpressionOperatorToken(SyntaxKind token)
        {
            return token switch
            {
                SyntaxKind.QuestionQuestionEqualsToken or SyntaxKind.BarEqualsToken or SyntaxKind.AmpersandEqualsToken or SyntaxKind.CaretEqualsToken or SyntaxKind.LessThanLessThanEqualsToken or SyntaxKind.GreaterThanGreaterThanEqualsToken or SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken or SyntaxKind.PlusEqualsToken or SyntaxKind.MinusEqualsToken or SyntaxKind.AsteriskEqualsToken or SyntaxKind.SlashEqualsToken or SyntaxKind.PercentEqualsToken or SyntaxKind.EqualsToken => true,
                _ => false,
            };
        }

        public static SyntaxKind GetAssignmentExpression(SyntaxKind token)
        {
            return token switch
            {
                SyntaxKind.BarEqualsToken => SyntaxKind.OrAssignmentExpression,
                SyntaxKind.AmpersandEqualsToken => SyntaxKind.AndAssignmentExpression,
                SyntaxKind.CaretEqualsToken => SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.LessThanLessThanEqualsToken => SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.GreaterThanGreaterThanEqualsToken => SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken => SyntaxKind.UnsignedRightShiftAssignmentExpression,
                SyntaxKind.PlusEqualsToken => SyntaxKind.AddAssignmentExpression,
                SyntaxKind.MinusEqualsToken => SyntaxKind.SubtractAssignmentExpression,
                SyntaxKind.AsteriskEqualsToken => SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.SlashEqualsToken => SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.PercentEqualsToken => SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.EqualsToken => SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.QuestionQuestionEqualsToken => SyntaxKind.CoalesceAssignmentExpression,
                _ => SyntaxKind.None,
            };
        }

        public static SyntaxKind GetCheckStatement(SyntaxKind keyword)
        {
            return keyword switch
            {
                SyntaxKind.CheckedKeyword => SyntaxKind.CheckedStatement,
                SyntaxKind.UncheckedKeyword => SyntaxKind.UncheckedStatement,
                _ => SyntaxKind.None,
            };
        }

        public static SyntaxKind GetAccessorDeclarationKind(SyntaxKind keyword)
        {
            return keyword switch
            {
                SyntaxKind.GetKeyword => SyntaxKind.GetAccessorDeclaration,
                SyntaxKind.SetKeyword => SyntaxKind.SetAccessorDeclaration,
                SyntaxKind.InitKeyword => SyntaxKind.InitAccessorDeclaration,
                SyntaxKind.AddKeyword => SyntaxKind.AddAccessorDeclaration,
                SyntaxKind.RemoveKeyword => SyntaxKind.RemoveAccessorDeclaration,
                _ => SyntaxKind.None,
            };
        }

        public static bool IsAccessorDeclaration(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration or SyntaxKind.AddAccessorDeclaration or SyntaxKind.RemoveAccessorDeclaration => true,
                _ => false,
            };
        }

        public static bool IsAccessorDeclarationKeyword(SyntaxKind keyword)
        {
            return keyword switch
            {
                SyntaxKind.GetKeyword or SyntaxKind.SetKeyword or SyntaxKind.InitKeyword or SyntaxKind.AddKeyword or SyntaxKind.RemoveKeyword => true,
                _ => false,
            };
        }

        public static SyntaxKind GetSwitchLabelKind(SyntaxKind keyword)
        {
            return keyword switch
            {
                SyntaxKind.CaseKeyword => SyntaxKind.CaseSwitchLabel,
                SyntaxKind.DefaultKeyword => SyntaxKind.DefaultSwitchLabel,
                _ => SyntaxKind.None,
            };
        }

        public static SyntaxKind GetBaseTypeDeclarationKind(SyntaxKind kind)
        {
            return kind == SyntaxKind.EnumKeyword ? SyntaxKind.EnumDeclaration : GetTypeDeclarationKind(kind);
        }

        public static SyntaxKind GetTypeDeclarationKind(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.ClassKeyword => SyntaxKind.ClassDeclaration,
                SyntaxKind.StructKeyword => SyntaxKind.StructDeclaration,
                SyntaxKind.InterfaceKeyword => SyntaxKind.InterfaceDeclaration,
                SyntaxKind.RecordKeyword => SyntaxKind.RecordDeclaration,
                _ => SyntaxKind.None,
            };
        }

        public static SyntaxKind GetKeywordKind(string text)
        {
            return text switch
            {
                "bool" => SyntaxKind.BoolKeyword,
                "byte" => SyntaxKind.ByteKeyword,
                "sbyte" => SyntaxKind.SByteKeyword,
                "short" => SyntaxKind.ShortKeyword,
                "ushort" => SyntaxKind.UShortKeyword,
                "int" => SyntaxKind.IntKeyword,
                "uint" => SyntaxKind.UIntKeyword,
                "long" => SyntaxKind.LongKeyword,
                "ulong" => SyntaxKind.ULongKeyword,
                "double" => SyntaxKind.DoubleKeyword,
                "float" => SyntaxKind.FloatKeyword,
                "decimal" => SyntaxKind.DecimalKeyword,
                "string" => SyntaxKind.StringKeyword,
                "char" => SyntaxKind.CharKeyword,
                "void" => SyntaxKind.VoidKeyword,
                "object" => SyntaxKind.ObjectKeyword,
                "typeof" => SyntaxKind.TypeOfKeyword,
                "sizeof" => SyntaxKind.SizeOfKeyword,
                "null" => SyntaxKind.NullKeyword,
                "true" => SyntaxKind.TrueKeyword,
                "false" => SyntaxKind.FalseKeyword,
                "if" => SyntaxKind.IfKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "while" => SyntaxKind.WhileKeyword,
                "for" => SyntaxKind.ForKeyword,
                "foreach" => SyntaxKind.ForEachKeyword,
                "do" => SyntaxKind.DoKeyword,
                "switch" => SyntaxKind.SwitchKeyword,
                "case" => SyntaxKind.CaseKeyword,
                "default" => SyntaxKind.DefaultKeyword,
                "lock" => SyntaxKind.LockKeyword,
                "try" => SyntaxKind.TryKeyword,
                "throw" => SyntaxKind.ThrowKeyword,
                "catch" => SyntaxKind.CatchKeyword,
                "finally" => SyntaxKind.FinallyKeyword,
                "goto" => SyntaxKind.GotoKeyword,
                "break" => SyntaxKind.BreakKeyword,
                "continue" => SyntaxKind.ContinueKeyword,
                "return" => SyntaxKind.ReturnKeyword,
                "public" => SyntaxKind.PublicKeyword,
                "private" => SyntaxKind.PrivateKeyword,
                "internal" => SyntaxKind.InternalKeyword,
                "protected" => SyntaxKind.ProtectedKeyword,
                "static" => SyntaxKind.StaticKeyword,
                "readonly" => SyntaxKind.ReadOnlyKeyword,
                "sealed" => SyntaxKind.SealedKeyword,
                "const" => SyntaxKind.ConstKeyword,
                "fixed" => SyntaxKind.FixedKeyword,
                "stackalloc" => SyntaxKind.StackAllocKeyword,
                "volatile" => SyntaxKind.VolatileKeyword,
                "new" => SyntaxKind.NewKeyword,
                "override" => SyntaxKind.OverrideKeyword,
                "abstract" => SyntaxKind.AbstractKeyword,
                "virtual" => SyntaxKind.VirtualKeyword,
                "event" => SyntaxKind.EventKeyword,
                "extern" => SyntaxKind.ExternKeyword,
                "ref" => SyntaxKind.RefKeyword,
                "out" => SyntaxKind.OutKeyword,
                "in" => SyntaxKind.InKeyword,
                "is" => SyntaxKind.IsKeyword,
                "as" => SyntaxKind.AsKeyword,
                "params" => SyntaxKind.ParamsKeyword,
                "__arglist" => SyntaxKind.ArgListKeyword,
                "__makeref" => SyntaxKind.MakeRefKeyword,
                "__reftype" => SyntaxKind.RefTypeKeyword,
                "__refvalue" => SyntaxKind.RefValueKeyword,
                "this" => SyntaxKind.ThisKeyword,
                "base" => SyntaxKind.BaseKeyword,
                "namespace" => SyntaxKind.NamespaceKeyword,
                "using" => SyntaxKind.UsingKeyword,
                "class" => SyntaxKind.ClassKeyword,
                "struct" => SyntaxKind.StructKeyword,
                "interface" => SyntaxKind.InterfaceKeyword,
                "enum" => SyntaxKind.EnumKeyword,
                "delegate" => SyntaxKind.DelegateKeyword,
                "checked" => SyntaxKind.CheckedKeyword,
                "unchecked" => SyntaxKind.UncheckedKeyword,
                "unsafe" => SyntaxKind.UnsafeKeyword,
                "operator" => SyntaxKind.OperatorKeyword,
                "implicit" => SyntaxKind.ImplicitKeyword,
                "explicit" => SyntaxKind.ExplicitKeyword,
                _ => SyntaxKind.None,
            };
        }

        public static SyntaxKind GetOperatorKind(string operatorMetadataName)
        {
            return operatorMetadataName switch
            {
                WellKnownMemberNames.CheckedAdditionOperatorName or WellKnownMemberNames.AdditionOperatorName => SyntaxKind.PlusToken,
                WellKnownMemberNames.BitwiseAndOperatorName => SyntaxKind.AmpersandToken,
                WellKnownMemberNames.BitwiseOrOperatorName => SyntaxKind.BarToken,
                // case WellKnownMemberNames.ConcatenateOperatorName:
                WellKnownMemberNames.CheckedDecrementOperatorName or WellKnownMemberNames.DecrementOperatorName => SyntaxKind.MinusMinusToken,
                WellKnownMemberNames.CheckedDivisionOperatorName or WellKnownMemberNames.DivisionOperatorName => SyntaxKind.SlashToken,
                WellKnownMemberNames.EqualityOperatorName => SyntaxKind.EqualsEqualsToken,
                WellKnownMemberNames.ExclusiveOrOperatorName => SyntaxKind.CaretToken,
                WellKnownMemberNames.CheckedExplicitConversionName or WellKnownMemberNames.ExplicitConversionName => SyntaxKind.ExplicitKeyword,
                // case WellKnownMemberNames.ExponentOperatorName:
                WellKnownMemberNames.FalseOperatorName => SyntaxKind.FalseKeyword,
                WellKnownMemberNames.GreaterThanOperatorName => SyntaxKind.GreaterThanToken,
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => SyntaxKind.GreaterThanEqualsToken,
                WellKnownMemberNames.ImplicitConversionName => SyntaxKind.ImplicitKeyword,
                WellKnownMemberNames.CheckedIncrementOperatorName or WellKnownMemberNames.IncrementOperatorName => SyntaxKind.PlusPlusToken,
                WellKnownMemberNames.InequalityOperatorName => SyntaxKind.ExclamationEqualsToken,
                //case WellKnownMemberNames.IntegerDivisionOperatorName: 
                WellKnownMemberNames.LeftShiftOperatorName => SyntaxKind.LessThanLessThanToken,
                WellKnownMemberNames.LessThanOperatorName => SyntaxKind.LessThanToken,
                WellKnownMemberNames.LessThanOrEqualOperatorName => SyntaxKind.LessThanEqualsToken,
                // case WellKnownMemberNames.LikeOperatorName:
                WellKnownMemberNames.LogicalNotOperatorName => SyntaxKind.ExclamationToken,
                WellKnownMemberNames.ModulusOperatorName => SyntaxKind.PercentToken,
                WellKnownMemberNames.CheckedMultiplyOperatorName or WellKnownMemberNames.MultiplyOperatorName => SyntaxKind.AsteriskToken,
                WellKnownMemberNames.OnesComplementOperatorName => SyntaxKind.TildeToken,
                WellKnownMemberNames.RightShiftOperatorName => SyntaxKind.GreaterThanGreaterThanToken,
                WellKnownMemberNames.UnsignedRightShiftOperatorName => SyntaxKind.GreaterThanGreaterThanGreaterThanToken,
                WellKnownMemberNames.CheckedSubtractionOperatorName or WellKnownMemberNames.SubtractionOperatorName => SyntaxKind.MinusToken,
                WellKnownMemberNames.TrueOperatorName => SyntaxKind.TrueKeyword,
                WellKnownMemberNames.CheckedUnaryNegationOperatorName or WellKnownMemberNames.UnaryNegationOperatorName => SyntaxKind.MinusToken,
                WellKnownMemberNames.UnaryPlusOperatorName => SyntaxKind.PlusToken,
                _ => SyntaxKind.None,
            };
        }

        public static bool IsCheckedOperator(string operatorMetadataName)
        {
            return operatorMetadataName switch
            {
                WellKnownMemberNames.CheckedDecrementOperatorName or WellKnownMemberNames.CheckedIncrementOperatorName or WellKnownMemberNames.CheckedUnaryNegationOperatorName or WellKnownMemberNames.CheckedAdditionOperatorName or WellKnownMemberNames.CheckedDivisionOperatorName or WellKnownMemberNames.CheckedMultiplyOperatorName or WellKnownMemberNames.CheckedSubtractionOperatorName or WellKnownMemberNames.CheckedExplicitConversionName => true,
                _ => false,
            };
        }

        public static SyntaxKind GetPreprocessorKeywordKind(string text)
        {
            return text switch
            {
                "true" => SyntaxKind.TrueKeyword,
                "false" => SyntaxKind.FalseKeyword,
                "default" => SyntaxKind.DefaultKeyword,
                "if" => SyntaxKind.IfKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "elif" => SyntaxKind.ElifKeyword,
                "endif" => SyntaxKind.EndIfKeyword,
                "region" => SyntaxKind.RegionKeyword,
                "endregion" => SyntaxKind.EndRegionKeyword,
                "define" => SyntaxKind.DefineKeyword,
                "undef" => SyntaxKind.UndefKeyword,
                "warning" => SyntaxKind.WarningKeyword,
                "error" => SyntaxKind.ErrorKeyword,
                "line" => SyntaxKind.LineKeyword,
                "pragma" => SyntaxKind.PragmaKeyword,
                "hidden" => SyntaxKind.HiddenKeyword,
                "checksum" => SyntaxKind.ChecksumKeyword,
                "disable" => SyntaxKind.DisableKeyword,
                "restore" => SyntaxKind.RestoreKeyword,
                "r" => SyntaxKind.ReferenceKeyword,
                "load" => SyntaxKind.LoadKeyword,
                "context" => SyntaxKind.ContextKeyword,
                "enable" => SyntaxKind.EnableKeyword,
                "warnings" => SyntaxKind.WarningsKeyword,
                "annotations" => SyntaxKind.AnnotationsKeyword,
                _ => SyntaxKind.None,
            };
        }

        public static IEnumerable<SyntaxKind> GetContextualKeywordKinds()
        {
            for (int i = (int)SyntaxKind.YieldKeyword; i <= (int)SyntaxKind.AllowsKeyword; i++)
            {
                // 8441 corresponds to a deleted kind (DataKeyword) that was previously shipped.
                if (i != 8441)
                {
                    Debug.Assert(Enum.IsDefined(typeof(SyntaxKind), (SyntaxKind)i));
                    yield return (SyntaxKind)i;
                }
            }
        }

        public static bool IsContextualKeyword(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.YieldKeyword or SyntaxKind.PartialKeyword or SyntaxKind.FromKeyword or SyntaxKind.GroupKeyword or SyntaxKind.JoinKeyword or SyntaxKind.IntoKeyword or SyntaxKind.LetKeyword or SyntaxKind.ByKeyword or SyntaxKind.WhereKeyword or SyntaxKind.SelectKeyword or SyntaxKind.GetKeyword or SyntaxKind.SetKeyword or SyntaxKind.AddKeyword or SyntaxKind.RemoveKeyword or SyntaxKind.OrderByKeyword or SyntaxKind.AliasKeyword or SyntaxKind.OnKeyword or SyntaxKind.EqualsKeyword or SyntaxKind.AscendingKeyword or SyntaxKind.DescendingKeyword or SyntaxKind.AssemblyKeyword or SyntaxKind.ModuleKeyword or SyntaxKind.TypeKeyword or SyntaxKind.GlobalKeyword or SyntaxKind.FieldKeyword or SyntaxKind.MethodKeyword or SyntaxKind.ParamKeyword or SyntaxKind.PropertyKeyword or SyntaxKind.TypeVarKeyword or SyntaxKind.NameOfKeyword or SyntaxKind.AsyncKeyword or SyntaxKind.AwaitKeyword or SyntaxKind.WhenKeyword or SyntaxKind.UnderscoreToken or SyntaxKind.VarKeyword or SyntaxKind.OrKeyword or SyntaxKind.AndKeyword or SyntaxKind.NotKeyword or SyntaxKind.WithKeyword or SyntaxKind.InitKeyword or SyntaxKind.RecordKeyword or SyntaxKind.ManagedKeyword or SyntaxKind.UnmanagedKeyword or SyntaxKind.RequiredKeyword or SyntaxKind.ScopedKeyword or SyntaxKind.FileKeyword or SyntaxKind.AllowsKeyword => true,
                _ => false,
            };
        }

        public static bool IsQueryContextualKeyword(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.FromKeyword or SyntaxKind.WhereKeyword or SyntaxKind.SelectKeyword or SyntaxKind.GroupKeyword or SyntaxKind.IntoKeyword or SyntaxKind.OrderByKeyword or SyntaxKind.JoinKeyword or SyntaxKind.LetKeyword or SyntaxKind.OnKeyword or SyntaxKind.EqualsKeyword or SyntaxKind.ByKeyword or SyntaxKind.AscendingKeyword or SyntaxKind.DescendingKeyword => true,
                _ => false,
            };
        }

        public static SyntaxKind GetContextualKeywordKind(string text)
        {
            return text switch
            {
                "yield" => SyntaxKind.YieldKeyword,
                "partial" => SyntaxKind.PartialKeyword,
                "from" => SyntaxKind.FromKeyword,
                "group" => SyntaxKind.GroupKeyword,
                "join" => SyntaxKind.JoinKeyword,
                "into" => SyntaxKind.IntoKeyword,
                "let" => SyntaxKind.LetKeyword,
                "by" => SyntaxKind.ByKeyword,
                "where" => SyntaxKind.WhereKeyword,
                "select" => SyntaxKind.SelectKeyword,
                "get" => SyntaxKind.GetKeyword,
                "set" => SyntaxKind.SetKeyword,
                "add" => SyntaxKind.AddKeyword,
                "remove" => SyntaxKind.RemoveKeyword,
                "orderby" => SyntaxKind.OrderByKeyword,
                "alias" => SyntaxKind.AliasKeyword,
                "on" => SyntaxKind.OnKeyword,
                "equals" => SyntaxKind.EqualsKeyword,
                "ascending" => SyntaxKind.AscendingKeyword,
                "descending" => SyntaxKind.DescendingKeyword,
                "assembly" => SyntaxKind.AssemblyKeyword,
                "module" => SyntaxKind.ModuleKeyword,
                "type" => SyntaxKind.TypeKeyword,
                "field" => SyntaxKind.FieldKeyword,
                "method" => SyntaxKind.MethodKeyword,
                "param" => SyntaxKind.ParamKeyword,
                "property" => SyntaxKind.PropertyKeyword,
                "typevar" => SyntaxKind.TypeVarKeyword,
                "global" => SyntaxKind.GlobalKeyword,
                "async" => SyntaxKind.AsyncKeyword,
                "await" => SyntaxKind.AwaitKeyword,
                "when" => SyntaxKind.WhenKeyword,
                "nameof" => SyntaxKind.NameOfKeyword,
                "_" => SyntaxKind.UnderscoreToken,
                "var" => SyntaxKind.VarKeyword,
                "and" => SyntaxKind.AndKeyword,
                "or" => SyntaxKind.OrKeyword,
                "not" => SyntaxKind.NotKeyword,
                "with" => SyntaxKind.WithKeyword,
                "init" => SyntaxKind.InitKeyword,
                "record" => SyntaxKind.RecordKeyword,
                "managed" => SyntaxKind.ManagedKeyword,
                "unmanaged" => SyntaxKind.UnmanagedKeyword,
                "required" => SyntaxKind.RequiredKeyword,
                "scoped" => SyntaxKind.ScopedKeyword,
                "file" => SyntaxKind.FileKeyword,
                "allows" => SyntaxKind.AllowsKeyword,
                _ => SyntaxKind.None,
            };
        }

        public static string GetText(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.TildeToken => "~",
                SyntaxKind.ExclamationToken => "!",
                SyntaxKind.DollarToken => "$",
                SyntaxKind.PercentToken => "%",
                SyntaxKind.CaretToken => "^",
                SyntaxKind.AmpersandToken => "&",
                SyntaxKind.AsteriskToken => "*",
                SyntaxKind.OpenParenToken => "(",
                SyntaxKind.CloseParenToken => ")",
                SyntaxKind.MinusToken => "-",
                SyntaxKind.PlusToken => "+",
                SyntaxKind.EqualsToken => "=",
                SyntaxKind.OpenBraceToken => "{",
                SyntaxKind.CloseBraceToken => "}",
                SyntaxKind.OpenBracketToken => "[",
                SyntaxKind.CloseBracketToken => "]",
                SyntaxKind.BarToken => "|",
                SyntaxKind.BackslashToken => "\\",
                SyntaxKind.ColonToken => ":",
                SyntaxKind.SemicolonToken => ";",
                SyntaxKind.DoubleQuoteToken => "\"",
                SyntaxKind.SingleQuoteToken => "'",
                SyntaxKind.LessThanToken => "<",
                SyntaxKind.CommaToken => ",",
                SyntaxKind.GreaterThanToken => ">",
                SyntaxKind.DotToken => ".",
                SyntaxKind.QuestionToken => "?",
                SyntaxKind.HashToken => "#",
                SyntaxKind.SlashToken => "/",
                SyntaxKind.SlashGreaterThanToken => "/>",
                SyntaxKind.LessThanSlashToken => "</",
                SyntaxKind.XmlCommentStartToken => "<!--",
                SyntaxKind.XmlCommentEndToken => "-->",
                SyntaxKind.XmlCDataStartToken => "<![CDATA[",
                SyntaxKind.XmlCDataEndToken => "]]>",
                SyntaxKind.XmlProcessingInstructionStartToken => "<?",
                SyntaxKind.XmlProcessingInstructionEndToken => "?>",
                // compound
                SyntaxKind.BarBarToken => "||",
                SyntaxKind.AmpersandAmpersandToken => "&&",
                SyntaxKind.MinusMinusToken => "--",
                SyntaxKind.PlusPlusToken => "++",
                SyntaxKind.ColonColonToken => "::",
                SyntaxKind.QuestionQuestionToken => "??",
                SyntaxKind.MinusGreaterThanToken => "->",
                SyntaxKind.ExclamationEqualsToken => "!=",
                SyntaxKind.EqualsEqualsToken => "==",
                SyntaxKind.EqualsGreaterThanToken => "=>",
                SyntaxKind.LessThanEqualsToken => "<=",
                SyntaxKind.LessThanLessThanToken => "<<",
                SyntaxKind.LessThanLessThanEqualsToken => "<<=",
                SyntaxKind.GreaterThanEqualsToken => ">=",
                SyntaxKind.GreaterThanGreaterThanToken => ">>",
                SyntaxKind.GreaterThanGreaterThanEqualsToken => ">>=",
                SyntaxKind.GreaterThanGreaterThanGreaterThanToken => ">>>",
                SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken => ">>>=",
                SyntaxKind.SlashEqualsToken => "/=",
                SyntaxKind.AsteriskEqualsToken => "*=",
                SyntaxKind.BarEqualsToken => "|=",
                SyntaxKind.AmpersandEqualsToken => "&=",
                SyntaxKind.PlusEqualsToken => "+=",
                SyntaxKind.MinusEqualsToken => "-=",
                SyntaxKind.CaretEqualsToken => "^=",
                SyntaxKind.PercentEqualsToken => "%=",
                SyntaxKind.QuestionQuestionEqualsToken => "??=",
                SyntaxKind.DotDotToken => "..",
                // Keywords
                SyntaxKind.BoolKeyword => "bool",
                SyntaxKind.ByteKeyword => "byte",
                SyntaxKind.SByteKeyword => "sbyte",
                SyntaxKind.ShortKeyword => "short",
                SyntaxKind.UShortKeyword => "ushort",
                SyntaxKind.IntKeyword => "int",
                SyntaxKind.UIntKeyword => "uint",
                SyntaxKind.LongKeyword => "long",
                SyntaxKind.ULongKeyword => "ulong",
                SyntaxKind.DoubleKeyword => "double",
                SyntaxKind.FloatKeyword => "float",
                SyntaxKind.DecimalKeyword => "decimal",
                SyntaxKind.StringKeyword => "string",
                SyntaxKind.CharKeyword => "char",
                SyntaxKind.VoidKeyword => "void",
                SyntaxKind.ObjectKeyword => "object",
                SyntaxKind.TypeOfKeyword => "typeof",
                SyntaxKind.SizeOfKeyword => "sizeof",
                SyntaxKind.NullKeyword => "null",
                SyntaxKind.TrueKeyword => "true",
                SyntaxKind.FalseKeyword => "false",
                SyntaxKind.IfKeyword => "if",
                SyntaxKind.ElseKeyword => "else",
                SyntaxKind.WhileKeyword => "while",
                SyntaxKind.ForKeyword => "for",
                SyntaxKind.ForEachKeyword => "foreach",
                SyntaxKind.DoKeyword => "do",
                SyntaxKind.SwitchKeyword => "switch",
                SyntaxKind.CaseKeyword => "case",
                SyntaxKind.DefaultKeyword => "default",
                SyntaxKind.TryKeyword => "try",
                SyntaxKind.CatchKeyword => "catch",
                SyntaxKind.FinallyKeyword => "finally",
                SyntaxKind.LockKeyword => "lock",
                SyntaxKind.GotoKeyword => "goto",
                SyntaxKind.BreakKeyword => "break",
                SyntaxKind.ContinueKeyword => "continue",
                SyntaxKind.ReturnKeyword => "return",
                SyntaxKind.ThrowKeyword => "throw",
                SyntaxKind.PublicKeyword => "public",
                SyntaxKind.PrivateKeyword => "private",
                SyntaxKind.InternalKeyword => "internal",
                SyntaxKind.ProtectedKeyword => "protected",
                SyntaxKind.StaticKeyword => "static",
                SyntaxKind.ReadOnlyKeyword => "readonly",
                SyntaxKind.SealedKeyword => "sealed",
                SyntaxKind.ConstKeyword => "const",
                SyntaxKind.FixedKeyword => "fixed",
                SyntaxKind.StackAllocKeyword => "stackalloc",
                SyntaxKind.VolatileKeyword => "volatile",
                SyntaxKind.NewKeyword => "new",
                SyntaxKind.OverrideKeyword => "override",
                SyntaxKind.AbstractKeyword => "abstract",
                SyntaxKind.VirtualKeyword => "virtual",
                SyntaxKind.EventKeyword => "event",
                SyntaxKind.ExternKeyword => "extern",
                SyntaxKind.RefKeyword => "ref",
                SyntaxKind.OutKeyword => "out",
                SyntaxKind.InKeyword => "in",
                SyntaxKind.IsKeyword => "is",
                SyntaxKind.AsKeyword => "as",
                SyntaxKind.ParamsKeyword => "params",
                SyntaxKind.ArgListKeyword => "__arglist",
                SyntaxKind.MakeRefKeyword => "__makeref",
                SyntaxKind.RefTypeKeyword => "__reftype",
                SyntaxKind.RefValueKeyword => "__refvalue",
                SyntaxKind.ThisKeyword => "this",
                SyntaxKind.BaseKeyword => "base",
                SyntaxKind.NamespaceKeyword => "namespace",
                SyntaxKind.UsingKeyword => "using",
                SyntaxKind.ClassKeyword => "class",
                SyntaxKind.StructKeyword => "struct",
                SyntaxKind.InterfaceKeyword => "interface",
                SyntaxKind.EnumKeyword => "enum",
                SyntaxKind.DelegateKeyword => "delegate",
                SyntaxKind.CheckedKeyword => "checked",
                SyntaxKind.UncheckedKeyword => "unchecked",
                SyntaxKind.UnsafeKeyword => "unsafe",
                SyntaxKind.OperatorKeyword => "operator",
                SyntaxKind.ImplicitKeyword => "implicit",
                SyntaxKind.ExplicitKeyword => "explicit",
                SyntaxKind.ElifKeyword => "elif",
                SyntaxKind.EndIfKeyword => "endif",
                SyntaxKind.RegionKeyword => "region",
                SyntaxKind.EndRegionKeyword => "endregion",
                SyntaxKind.DefineKeyword => "define",
                SyntaxKind.UndefKeyword => "undef",
                SyntaxKind.WarningKeyword => "warning",
                SyntaxKind.ErrorKeyword => "error",
                SyntaxKind.LineKeyword => "line",
                SyntaxKind.PragmaKeyword => "pragma",
                SyntaxKind.HiddenKeyword => "hidden",
                SyntaxKind.ChecksumKeyword => "checksum",
                SyntaxKind.DisableKeyword => "disable",
                SyntaxKind.RestoreKeyword => "restore",
                SyntaxKind.ReferenceKeyword => "r",
                SyntaxKind.LoadKeyword => "load",
                SyntaxKind.ContextKeyword => "context",
                SyntaxKind.EnableKeyword => "enable",
                SyntaxKind.WarningsKeyword => "warnings",
                SyntaxKind.AnnotationsKeyword => "annotations",
                // contextual keywords
                SyntaxKind.YieldKeyword => "yield",
                SyntaxKind.PartialKeyword => "partial",
                SyntaxKind.FromKeyword => "from",
                SyntaxKind.GroupKeyword => "group",
                SyntaxKind.JoinKeyword => "join",
                SyntaxKind.IntoKeyword => "into",
                SyntaxKind.LetKeyword => "let",
                SyntaxKind.ByKeyword => "by",
                SyntaxKind.WhereKeyword => "where",
                SyntaxKind.SelectKeyword => "select",
                SyntaxKind.GetKeyword => "get",
                SyntaxKind.SetKeyword => "set",
                SyntaxKind.AddKeyword => "add",
                SyntaxKind.RemoveKeyword => "remove",
                SyntaxKind.OrderByKeyword => "orderby",
                SyntaxKind.AliasKeyword => "alias",
                SyntaxKind.OnKeyword => "on",
                SyntaxKind.EqualsKeyword => "equals",
                SyntaxKind.AscendingKeyword => "ascending",
                SyntaxKind.DescendingKeyword => "descending",
                SyntaxKind.AssemblyKeyword => "assembly",
                SyntaxKind.ModuleKeyword => "module",
                SyntaxKind.TypeKeyword => "type",
                SyntaxKind.FieldKeyword => "field",
                SyntaxKind.MethodKeyword => "method",
                SyntaxKind.ParamKeyword => "param",
                SyntaxKind.PropertyKeyword => "property",
                SyntaxKind.TypeVarKeyword => "typevar",
                SyntaxKind.GlobalKeyword => "global",
                SyntaxKind.NameOfKeyword => "nameof",
                SyntaxKind.AsyncKeyword => "async",
                SyntaxKind.AwaitKeyword => "await",
                SyntaxKind.WhenKeyword => "when",
                SyntaxKind.InterpolatedStringStartToken => "$\"",
                SyntaxKind.InterpolatedStringEndToken => "\"",
                SyntaxKind.InterpolatedVerbatimStringStartToken => "$@\"",
                SyntaxKind.UnderscoreToken => "_",
                SyntaxKind.VarKeyword => "var",
                SyntaxKind.AndKeyword => "and",
                SyntaxKind.OrKeyword => "or",
                SyntaxKind.NotKeyword => "not",
                SyntaxKind.WithKeyword => "with",
                SyntaxKind.InitKeyword => "init",
                SyntaxKind.RecordKeyword => "record",
                SyntaxKind.ManagedKeyword => "managed",
                SyntaxKind.UnmanagedKeyword => "unmanaged",
                SyntaxKind.RequiredKeyword => "required",
                SyntaxKind.ScopedKeyword => "scoped",
                SyntaxKind.FileKeyword => "file",
                SyntaxKind.AllowsKeyword => "allows",
                _ => string.Empty,
            };
        }

        public static bool IsTypeParameterVarianceKeyword(SyntaxKind kind)
        {
            return kind == SyntaxKind.OutKeyword || kind == SyntaxKind.InKeyword;
        }

        public static bool IsDocumentationCommentTrivia(SyntaxKind kind)
        {
            return kind == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                kind == SyntaxKind.MultiLineDocumentationCommentTrivia;
        }
    }
}
