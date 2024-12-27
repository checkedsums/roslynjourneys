// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class OperatorKindExtensions
    {
        public static int OperatorIndex(this UnaryOperatorKind kind)
        {
            return ((int)kind.Operator() >> 8) - 16;
        }

        public static UnaryOperatorKind Operator(this UnaryOperatorKind kind)
        {
            return kind & UnaryOperatorKind.OpMask;
        }

        public static UnaryOperatorKind Unlifted(this UnaryOperatorKind kind)
        {
            return kind & ~UnaryOperatorKind.Lifted;
        }

        public static bool IsLifted(this UnaryOperatorKind kind)
        {
            return 0 != (kind & UnaryOperatorKind.Lifted);
        }

        public static bool IsChecked(this UnaryOperatorKind kind)
        {
            return 0 != (kind & UnaryOperatorKind.Checked);
        }

        public static bool IsUserDefined(this UnaryOperatorKind kind)
        {
            return (kind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;
        }

        public static UnaryOperatorKind OverflowChecks(this UnaryOperatorKind kind)
        {
            return kind & UnaryOperatorKind.Checked;
        }

        public static UnaryOperatorKind WithOverflowChecksIfApplicable(this UnaryOperatorKind kind, bool enabled)
        {
            if (enabled)
            {
                // If it's dynamic and we're in a checked context then just mark it as checked,
                // regardless of whether it is +x -x !x ~x ++x --x x++ or x--. Let the lowering 
                // pass sort out what to do with it.

                if (kind.IsDynamic())
                {
                    return kind | UnaryOperatorKind.Checked;
                }

                if (kind.IsIntegral())
                {
                    switch (kind.Operator())
                    {
                        case UnaryOperatorKind.PrefixIncrement:
                        case UnaryOperatorKind.PostfixIncrement:
                        case UnaryOperatorKind.PrefixDecrement:
                        case UnaryOperatorKind.PostfixDecrement:
                        case UnaryOperatorKind.UnaryMinus:
                            return kind | UnaryOperatorKind.Checked;
                    }
                }
                return kind;
            }
            else
            {
                return kind & ~UnaryOperatorKind.Checked;
            }
        }

        public static UnaryOperatorKind OperandTypes(this UnaryOperatorKind kind)
        {
            return kind & UnaryOperatorKind.TypeMask;
        }

        public static bool IsDynamic(this UnaryOperatorKind kind)
        {
            return kind.OperandTypes() == UnaryOperatorKind.Dynamic;
        }

        public static bool IsIntegral(this UnaryOperatorKind kind)
        {
            return kind.OperandTypes() switch
            {
                UnaryOperatorKind.SByte or UnaryOperatorKind.Byte or UnaryOperatorKind.Short or UnaryOperatorKind.UShort or UnaryOperatorKind.Int or UnaryOperatorKind.UInt or UnaryOperatorKind.Long or UnaryOperatorKind.ULong or UnaryOperatorKind.NInt or UnaryOperatorKind.NUInt or UnaryOperatorKind.Char or UnaryOperatorKind.Enum or UnaryOperatorKind.Pointer => true,
                _ => false,
            };
        }

        public static UnaryOperatorKind WithType(this UnaryOperatorKind kind, UnaryOperatorKind type)
        {
            Debug.Assert(kind == (kind & ~UnaryOperatorKind.TypeMask));
            Debug.Assert(type == (type & UnaryOperatorKind.TypeMask));
            return kind | type;
        }

        public static int OperatorIndex(this BinaryOperatorKind kind)
        {
            return ((int)kind.Operator() >> 8) - 16;
        }

        public static BinaryOperatorKind Operator(this BinaryOperatorKind kind)
        {
            return kind & BinaryOperatorKind.OpMask;
        }

        public static BinaryOperatorKind Unlifted(this BinaryOperatorKind kind)
        {
            return kind & ~BinaryOperatorKind.Lifted;
        }

        public static BinaryOperatorKind OperatorWithLogical(this BinaryOperatorKind kind)
        {
            return kind & (BinaryOperatorKind.OpMask | BinaryOperatorKind.Logical);
        }

        public static BinaryOperatorKind WithType(this BinaryOperatorKind kind, SpecialType type)
        {
            Debug.Assert(kind == (kind & ~BinaryOperatorKind.TypeMask));
            return type switch
            {
                SpecialType.System_Int32 => kind | BinaryOperatorKind.Int,
                SpecialType.System_UInt32 => kind | BinaryOperatorKind.UInt,
                SpecialType.System_Int64 => kind | BinaryOperatorKind.Long,
                SpecialType.System_UInt64 => kind | BinaryOperatorKind.ULong,
                _ => throw ExceptionUtilities.UnexpectedValue(type),
            };
        }

        public static UnaryOperatorKind WithType(this UnaryOperatorKind kind, SpecialType type)
        {
            Debug.Assert(kind == (kind & ~UnaryOperatorKind.TypeMask));
            return type switch
            {
                SpecialType.System_Int32 => kind | UnaryOperatorKind.Int,
                SpecialType.System_UInt32 => kind | UnaryOperatorKind.UInt,
                SpecialType.System_Int64 => kind | UnaryOperatorKind.Long,
                SpecialType.System_UInt64 => kind | UnaryOperatorKind.ULong,
                _ => throw ExceptionUtilities.UnexpectedValue(type),
            };
        }

        public static BinaryOperatorKind WithType(this BinaryOperatorKind kind, BinaryOperatorKind type)
        {
            Debug.Assert(kind == (kind & ~BinaryOperatorKind.TypeMask));
            Debug.Assert(type == (type & BinaryOperatorKind.TypeMask));
            return kind | type;
        }

        public static bool IsLifted(this BinaryOperatorKind kind)
        {
            return 0 != (kind & BinaryOperatorKind.Lifted);
        }

        public static bool IsDynamic(this BinaryOperatorKind kind)
        {
            return kind.OperandTypes() == BinaryOperatorKind.Dynamic;
        }

        public static bool IsComparison(this BinaryOperatorKind kind)
        {
            return kind.Operator() switch
            {
                BinaryOperatorKind.Equal or BinaryOperatorKind.NotEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual => true,
                _ => false,
            };
        }

        public static bool IsChecked(this BinaryOperatorKind kind)
        {
            return 0 != (kind & BinaryOperatorKind.Checked);
        }

        public static bool EmitsAsCheckedInstruction(this BinaryOperatorKind kind)
        {
            if (!kind.IsChecked())
            {
                return false;
            }

            return kind.Operator() switch
            {
                BinaryOperatorKind.Addition or BinaryOperatorKind.Subtraction or BinaryOperatorKind.Multiplication => true,
                _ => false,
            };
        }

        public static BinaryOperatorKind WithOverflowChecksIfApplicable(this BinaryOperatorKind kind, bool enabled)
        {
            if (enabled)
            {
                // If it's a dynamic binop then make it checked. Let the lowering
                // pass sort out what to do with it.
                if (kind.IsDynamic())
                {
                    return kind | BinaryOperatorKind.Checked;
                }

                if (kind.IsIntegral())
                {
                    switch (kind.Operator())
                    {
                        case BinaryOperatorKind.Addition:
                        case BinaryOperatorKind.Subtraction:
                        case BinaryOperatorKind.Multiplication:
                        case BinaryOperatorKind.Division:
                            return kind | BinaryOperatorKind.Checked;
                    }
                }
                return kind;
            }
            else
            {
                return kind & ~BinaryOperatorKind.Checked;
            }
        }

        public static bool IsEnum(this BinaryOperatorKind kind)
        {
            return kind.OperandTypes() switch
            {
                BinaryOperatorKind.Enum or BinaryOperatorKind.EnumAndUnderlying or BinaryOperatorKind.UnderlyingAndEnum => true,
                _ => false,
            };
        }

        public static bool IsEnum(this UnaryOperatorKind kind)
        {
            return kind.OperandTypes() == UnaryOperatorKind.Enum;
        }

        public static bool IsIntegral(this BinaryOperatorKind kind)
        {
            return kind.OperandTypes() switch
            {
                BinaryOperatorKind.Int or BinaryOperatorKind.UInt or BinaryOperatorKind.Long or BinaryOperatorKind.ULong or BinaryOperatorKind.NInt or BinaryOperatorKind.NUInt or BinaryOperatorKind.Char or BinaryOperatorKind.Enum or BinaryOperatorKind.EnumAndUnderlying or BinaryOperatorKind.UnderlyingAndEnum or BinaryOperatorKind.Pointer or BinaryOperatorKind.PointerAndInt or BinaryOperatorKind.PointerAndUInt or BinaryOperatorKind.PointerAndLong or BinaryOperatorKind.PointerAndULong or BinaryOperatorKind.IntAndPointer or BinaryOperatorKind.UIntAndPointer or BinaryOperatorKind.LongAndPointer or BinaryOperatorKind.ULongAndPointer => true,
                _ => false,
            };
        }

        public static bool IsLogical(this BinaryOperatorKind kind)
        {
            return 0 != (kind & BinaryOperatorKind.Logical);
        }

        public static BinaryOperatorKind OperandTypes(this BinaryOperatorKind kind)
        {
            return kind & BinaryOperatorKind.TypeMask;
        }

        public static bool IsUserDefined(this BinaryOperatorKind kind)
        {
            return (kind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;
        }

        public static bool IsShift(this BinaryOperatorKind kind)
        {
            BinaryOperatorKind type = kind.Operator();
            return type == BinaryOperatorKind.LeftShift || type == BinaryOperatorKind.RightShift || type == BinaryOperatorKind.UnsignedRightShift;
        }

        public static ExpressionType ToExpressionType(this BinaryOperatorKind kind, bool isCompoundAssignment)
        {
            if (isCompoundAssignment)
            {
                switch (kind.Operator())
                {
                    case BinaryOperatorKind.Multiplication: return ExpressionType.MultiplyAssign;
                    case BinaryOperatorKind.Addition: return ExpressionType.AddAssign;
                    case BinaryOperatorKind.Subtraction: return ExpressionType.SubtractAssign;
                    case BinaryOperatorKind.Division: return ExpressionType.DivideAssign;
                    case BinaryOperatorKind.Remainder: return ExpressionType.ModuloAssign;
                    case BinaryOperatorKind.LeftShift: return ExpressionType.LeftShiftAssign;
                    case BinaryOperatorKind.RightShift: return ExpressionType.RightShiftAssign;
                    case BinaryOperatorKind.And: return ExpressionType.AndAssign;
                    case BinaryOperatorKind.Xor: return ExpressionType.ExclusiveOrAssign;
                    case BinaryOperatorKind.Or: return ExpressionType.OrAssign;
                }
            }
            else
            {
                switch (kind.Operator())
                {
                    case BinaryOperatorKind.Multiplication: return ExpressionType.Multiply;
                    case BinaryOperatorKind.Addition: return ExpressionType.Add;
                    case BinaryOperatorKind.Subtraction: return ExpressionType.Subtract;
                    case BinaryOperatorKind.Division: return ExpressionType.Divide;
                    case BinaryOperatorKind.Remainder: return ExpressionType.Modulo;
                    case BinaryOperatorKind.LeftShift: return ExpressionType.LeftShift;
                    case BinaryOperatorKind.RightShift: return ExpressionType.RightShift;
                    case BinaryOperatorKind.Equal: return ExpressionType.Equal;
                    case BinaryOperatorKind.NotEqual: return ExpressionType.NotEqual;
                    case BinaryOperatorKind.GreaterThan: return ExpressionType.GreaterThan;
                    case BinaryOperatorKind.LessThan: return ExpressionType.LessThan;
                    case BinaryOperatorKind.GreaterThanOrEqual: return ExpressionType.GreaterThanOrEqual;
                    case BinaryOperatorKind.LessThanOrEqual: return ExpressionType.LessThanOrEqual;
                    case BinaryOperatorKind.And: return ExpressionType.And;
                    case BinaryOperatorKind.Xor: return ExpressionType.ExclusiveOr;
                    case BinaryOperatorKind.Or: return ExpressionType.Or;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(kind.Operator());
        }

        public static ExpressionType ToExpressionType(this UnaryOperatorKind kind)
        {
            return kind.Operator() switch
            {
                UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PostfixIncrement => ExpressionType.Increment,
                UnaryOperatorKind.PostfixDecrement or UnaryOperatorKind.PrefixDecrement => ExpressionType.Decrement,
                UnaryOperatorKind.UnaryPlus => ExpressionType.UnaryPlus,
                UnaryOperatorKind.UnaryMinus => ExpressionType.Negate,
                UnaryOperatorKind.LogicalNegation => ExpressionType.Not,
                UnaryOperatorKind.BitwiseComplement => ExpressionType.OnesComplement,
                UnaryOperatorKind.True => ExpressionType.IsTrue,
                UnaryOperatorKind.False => ExpressionType.IsFalse,
                _ => throw ExceptionUtilities.UnexpectedValue(kind.Operator()),
            };
        }

#if DEBUG
        public static string Dump(this BinaryOperatorKind kind)
        {
            var b = new StringBuilder();
            if ((kind & BinaryOperatorKind.Lifted) != 0) b.Append("Lifted");
            if ((kind & BinaryOperatorKind.Logical) != 0) b.Append("Logical");
            if ((kind & BinaryOperatorKind.Checked) != 0) b.Append("Checked");
            var type = kind & BinaryOperatorKind.TypeMask;
            if (type != 0) b.Append(type.ToString());
            var op = kind & BinaryOperatorKind.OpMask;
            if (op != 0) b.Append(op.ToString());
            return b.ToString();
        }

        public static string Dump(this UnaryOperatorKind kind)
        {
            var b = new StringBuilder();
            if ((kind & UnaryOperatorKind.Lifted) != 0) b.Append("Lifted");
            if ((kind & UnaryOperatorKind.Checked) != 0) b.Append("Checked");
            var type = kind & UnaryOperatorKind.TypeMask;
            if (type != 0) b.Append(type.ToString());
            var op = kind & UnaryOperatorKind.OpMask;
            if (op != 0) b.Append(op.ToString());
            return b.ToString();
        }
#endif
    }
}
