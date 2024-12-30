// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class SyntaxKindExtensions
    {
        internal static SpecialType GetSpecialType(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.VoidKeyword => SpecialType.System_Void,
            SyntaxKind.BoolKeyword => SpecialType.System_Boolean,
            SyntaxKind.ByteKeyword => SpecialType.System_Byte,
            SyntaxKind.SByteKeyword => SpecialType.System_SByte,
            SyntaxKind.ShortKeyword => SpecialType.System_Int16,
            SyntaxKind.UShortKeyword => SpecialType.System_UInt16,
            SyntaxKind.IntKeyword => SpecialType.System_Int32,
            SyntaxKind.UIntKeyword => SpecialType.System_UInt32,
            SyntaxKind.LongKeyword => SpecialType.System_Int64,
            SyntaxKind.ULongKeyword => SpecialType.System_UInt64,
            SyntaxKind.DoubleKeyword => SpecialType.System_Double,
            SyntaxKind.FloatKeyword => SpecialType.System_Single,
            SyntaxKind.DecimalKeyword => SpecialType.System_Decimal,
            SyntaxKind.StringKeyword => SpecialType.System_String,
            SyntaxKind.CharKeyword => SpecialType.System_Char,
            SyntaxKind.ObjectKeyword => SpecialType.System_Object,
            _ => throw ExceptionUtilities.UnexpectedValue(kind),// Note that "dynamic" is a contextual keyword, so it should never show up here.
        };

        public static SyntaxKind getCongener(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.CloseParenToken => SyntaxKind.OpenParenToken,
            SyntaxKind.OpenParenToken => SyntaxKind.CloseParenToken,
            SyntaxKind.CloseBraceToken => SyntaxKind.OpenBraceToken,
            SyntaxKind.OpenBraceToken => SyntaxKind.CloseBraceToken,
            SyntaxKind.CloseBracketToken => SyntaxKind.OpenBracketToken,
            SyntaxKind.OpenBracketToken => SyntaxKind.CloseBracketToken,
            _ => throw ExceptionUtilities.UnexpectedValue(nameof(kind)),
        };
    }
}
