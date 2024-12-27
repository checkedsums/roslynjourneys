// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class OperatorFacts
    {
        public static bool DefinitelyHasNoUserDefinedOperators(TypeSymbol type)
        {
            // We can take an early out and not look for user-defined operators.

            switch (type.TypeKind)
            {
                case TypeKind.Struct:
                case TypeKind.Class:
                case TypeKind.TypeParameter:
                case TypeKind.Interface:
                    break;
                default:
                    return true;
            }

            // System.Decimal does have user-defined operators but it is treated as 
            // though it were a built-in type.
            switch (type.SpecialType)
            {
                case SpecialType.System_Array:
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Decimal:
                case SpecialType.System_Delegate:
                case SpecialType.System_Double:
                case SpecialType.System_Enum:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr when type.IsNativeIntegerType:
                case SpecialType.System_UIntPtr when type.IsNativeIntegerType:
                case SpecialType.System_MulticastDelegate:
                case SpecialType.System_Object:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                case SpecialType.System_String:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_ValueType:
                case SpecialType.System_Void:
                    return true;
            }

            return false;
        }

        public static string BinaryOperatorNameFromSyntaxKind(SyntaxKind kind, bool isChecked)
        {
            return BinaryOperatorNameFromSyntaxKindIfAny(kind, isChecked) ??
                (isChecked ? WellKnownMemberNames.CheckedAdditionOperatorName : WellKnownMemberNames.AdditionOperatorName); // This can occur in the presence of syntax errors.
        }

        internal static string BinaryOperatorNameFromSyntaxKindIfAny(SyntaxKind kind, bool isChecked)
        {
            return kind switch
            {
                SyntaxKind.PlusToken => isChecked ? WellKnownMemberNames.CheckedAdditionOperatorName : WellKnownMemberNames.AdditionOperatorName,
                SyntaxKind.MinusToken => isChecked ? WellKnownMemberNames.CheckedSubtractionOperatorName : WellKnownMemberNames.SubtractionOperatorName,
                SyntaxKind.AsteriskToken => isChecked ? WellKnownMemberNames.CheckedMultiplyOperatorName : WellKnownMemberNames.MultiplyOperatorName,
                SyntaxKind.SlashToken => isChecked ? WellKnownMemberNames.CheckedDivisionOperatorName : WellKnownMemberNames.DivisionOperatorName,
                SyntaxKind.PercentToken => WellKnownMemberNames.ModulusOperatorName,
                SyntaxKind.CaretToken => WellKnownMemberNames.ExclusiveOrOperatorName,
                SyntaxKind.AmpersandToken => WellKnownMemberNames.BitwiseAndOperatorName,
                SyntaxKind.BarToken => WellKnownMemberNames.BitwiseOrOperatorName,
                SyntaxKind.EqualsEqualsToken => WellKnownMemberNames.EqualityOperatorName,
                SyntaxKind.LessThanToken => WellKnownMemberNames.LessThanOperatorName,
                SyntaxKind.LessThanEqualsToken => WellKnownMemberNames.LessThanOrEqualOperatorName,
                SyntaxKind.LessThanLessThanToken => WellKnownMemberNames.LeftShiftOperatorName,
                SyntaxKind.GreaterThanToken => WellKnownMemberNames.GreaterThanOperatorName,
                SyntaxKind.GreaterThanEqualsToken => WellKnownMemberNames.GreaterThanOrEqualOperatorName,
                SyntaxKind.GreaterThanGreaterThanToken => WellKnownMemberNames.RightShiftOperatorName,
                SyntaxKind.GreaterThanGreaterThanGreaterThanToken => WellKnownMemberNames.UnsignedRightShiftOperatorName,
                SyntaxKind.ExclamationEqualsToken => WellKnownMemberNames.InequalityOperatorName,
                _ => null,
            };
        }

        public static string UnaryOperatorNameFromSyntaxKind(SyntaxKind kind, bool isChecked)
        {
            return UnaryOperatorNameFromSyntaxKindIfAny(kind, isChecked) ??
                WellKnownMemberNames.UnaryPlusOperatorName; // This can occur in the presence of syntax errors.
        }

        internal static string UnaryOperatorNameFromSyntaxKindIfAny(SyntaxKind kind, bool isChecked)
        {
            return kind switch
            {
                SyntaxKind.PlusToken => WellKnownMemberNames.UnaryPlusOperatorName,
                SyntaxKind.MinusToken => isChecked ? WellKnownMemberNames.CheckedUnaryNegationOperatorName : WellKnownMemberNames.UnaryNegationOperatorName,
                SyntaxKind.TildeToken => WellKnownMemberNames.OnesComplementOperatorName,
                SyntaxKind.ExclamationToken => WellKnownMemberNames.LogicalNotOperatorName,
                SyntaxKind.PlusPlusToken => isChecked ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.IncrementOperatorName,
                SyntaxKind.MinusMinusToken => isChecked ? WellKnownMemberNames.CheckedDecrementOperatorName : WellKnownMemberNames.DecrementOperatorName,
                SyntaxKind.TrueKeyword => WellKnownMemberNames.TrueOperatorName,
                SyntaxKind.FalseKeyword => WellKnownMemberNames.FalseOperatorName,
                _ => null,
            };
        }

        public static string OperatorNameFromDeclaration(OperatorDeclarationSyntax declaration)
        {
            return OperatorNameFromDeclaration((Syntax.InternalSyntax.OperatorDeclarationSyntax)(declaration.Green));
        }

        public static string OperatorNameFromDeclaration(Syntax.InternalSyntax.OperatorDeclarationSyntax declaration)
        {
            var opTokenKind = declaration.OperatorToken.Kind;
            bool isChecked = declaration.CheckedKeyword?.Kind == SyntaxKind.CheckedKeyword;

            if (SyntaxFacts.IsBinaryExpressionOperatorToken(opTokenKind))
            {
                // Some tokens may be either unary or binary operators (e.g. +, -).
                if (opTokenKind != SyntaxKind.AsteriskToken && // IsPrefixUnaryExpressionOperatorToken treats it as pointer dereference operator
                    SyntaxFacts.IsPrefixUnaryExpressionOperatorToken(opTokenKind) &&
                    declaration.ParameterList.Parameters.Count == 1)
                {
                    return OperatorFacts.UnaryOperatorNameFromSyntaxKind(opTokenKind, isChecked);
                }

                return OperatorFacts.BinaryOperatorNameFromSyntaxKind(opTokenKind, isChecked);
            }
            else if (SyntaxFacts.IsUnaryOperatorDeclarationToken(opTokenKind))
            {
                return OperatorFacts.UnaryOperatorNameFromSyntaxKind(opTokenKind, isChecked);
            }
            else
            {
                // fallback for error recovery
                return WellKnownMemberNames.UnaryPlusOperatorName;
            }
        }

        public static string OperatorNameFromDeclaration(ConversionOperatorDeclarationSyntax declaration)
        {
            return OperatorNameFromDeclaration((Syntax.InternalSyntax.ConversionOperatorDeclarationSyntax)(declaration.Green));
        }

        public static string OperatorNameFromDeclaration(Syntax.InternalSyntax.ConversionOperatorDeclarationSyntax declaration)
        {
            return declaration.ImplicitOrExplicitKeyword.Kind switch
            {
                SyntaxKind.ImplicitKeyword => WellKnownMemberNames.ImplicitConversionName,
                _ => declaration.CheckedKeyword?.Kind == SyntaxKind.CheckedKeyword ?
                                                WellKnownMemberNames.CheckedExplicitConversionName :
                                                WellKnownMemberNames.ExplicitConversionName,
            };
        }

        public static string UnaryOperatorNameFromOperatorKind(UnaryOperatorKind kind, bool isChecked)
        {
            return (kind & UnaryOperatorKind.OpMask) switch
            {
                UnaryOperatorKind.UnaryPlus => WellKnownMemberNames.UnaryPlusOperatorName,
                UnaryOperatorKind.UnaryMinus => isChecked ? WellKnownMemberNames.CheckedUnaryNegationOperatorName : WellKnownMemberNames.UnaryNegationOperatorName,
                UnaryOperatorKind.BitwiseComplement => WellKnownMemberNames.OnesComplementOperatorName,
                UnaryOperatorKind.LogicalNegation => WellKnownMemberNames.LogicalNotOperatorName,
                UnaryOperatorKind.PostfixIncrement or UnaryOperatorKind.PrefixIncrement => isChecked ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.IncrementOperatorName,
                UnaryOperatorKind.PostfixDecrement or UnaryOperatorKind.PrefixDecrement => isChecked ? WellKnownMemberNames.CheckedDecrementOperatorName : WellKnownMemberNames.DecrementOperatorName,
                UnaryOperatorKind.True => WellKnownMemberNames.TrueOperatorName,
                UnaryOperatorKind.False => WellKnownMemberNames.FalseOperatorName,
                _ => throw ExceptionUtilities.UnexpectedValue(kind & UnaryOperatorKind.OpMask),
            };
        }

        public static string BinaryOperatorNameFromOperatorKind(BinaryOperatorKind kind, bool isChecked)
        {
            return (kind & BinaryOperatorKind.OpMask) switch
            {
                BinaryOperatorKind.Addition => isChecked ? WellKnownMemberNames.CheckedAdditionOperatorName : WellKnownMemberNames.AdditionOperatorName,
                BinaryOperatorKind.And => WellKnownMemberNames.BitwiseAndOperatorName,
                BinaryOperatorKind.Division => isChecked ? WellKnownMemberNames.CheckedDivisionOperatorName : WellKnownMemberNames.DivisionOperatorName,
                BinaryOperatorKind.Equal => WellKnownMemberNames.EqualityOperatorName,
                BinaryOperatorKind.GreaterThan => WellKnownMemberNames.GreaterThanOperatorName,
                BinaryOperatorKind.GreaterThanOrEqual => WellKnownMemberNames.GreaterThanOrEqualOperatorName,
                BinaryOperatorKind.LeftShift => WellKnownMemberNames.LeftShiftOperatorName,
                BinaryOperatorKind.LessThan => WellKnownMemberNames.LessThanOperatorName,
                BinaryOperatorKind.LessThanOrEqual => WellKnownMemberNames.LessThanOrEqualOperatorName,
                BinaryOperatorKind.Multiplication => isChecked ? WellKnownMemberNames.CheckedMultiplyOperatorName : WellKnownMemberNames.MultiplyOperatorName,
                BinaryOperatorKind.Or => WellKnownMemberNames.BitwiseOrOperatorName,
                BinaryOperatorKind.NotEqual => WellKnownMemberNames.InequalityOperatorName,
                BinaryOperatorKind.Remainder => WellKnownMemberNames.ModulusOperatorName,
                BinaryOperatorKind.RightShift => WellKnownMemberNames.RightShiftOperatorName,
                BinaryOperatorKind.UnsignedRightShift => WellKnownMemberNames.UnsignedRightShiftOperatorName,
                BinaryOperatorKind.Subtraction => isChecked ? WellKnownMemberNames.CheckedSubtractionOperatorName : WellKnownMemberNames.SubtractionOperatorName,
                BinaryOperatorKind.Xor => WellKnownMemberNames.ExclusiveOrOperatorName,
                _ => throw ExceptionUtilities.UnexpectedValue(kind & BinaryOperatorKind.OpMask),
            };
        }
    }
}
