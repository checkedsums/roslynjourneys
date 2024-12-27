// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.ConversionKind;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class ConversionKindExtensions
    {
        public static bool IsDynamic(this ConversionKind conversionKind)
        {
            return conversionKind == ImplicitDynamic || conversionKind == ExplicitDynamic;
        }

        // Is the particular conversion an implicit conversion?
        public static bool IsImplicitConversion(this ConversionKind conversionKind)
        {
            return conversionKind switch
            {
                NoConversion or UnsetConversionKind => false,
                Identity or ImplicitNumeric or ImplicitTupleLiteral or ImplicitTuple or ImplicitEnumeration or ImplicitThrow or ImplicitNullable or NullLiteral or DefaultLiteral or ImplicitReference or Boxing or ImplicitDynamic or ImplicitConstant or ImplicitUserDefined or AnonymousFunction or ConversionKind.MethodGroup or ConversionKind.FunctionType or ImplicitPointerToVoid or ImplicitNullToPointer or InterpolatedString or InterpolatedStringHandler or SwitchExpression or ConditionalExpression or Deconstruction or StackAllocToPointerType or StackAllocToSpanType or ImplicitPointer or ObjectCreation or InlineArray or CollectionExpression or ImplicitSpan => true,
                ExplicitNumeric or ExplicitTuple or ExplicitTupleLiteral or ExplicitEnumeration or ExplicitNullable or ExplicitReference or Unboxing or ExplicitDynamic or ExplicitUserDefined or ExplicitPointerToPointer or ExplicitPointerToInteger or ExplicitIntegerToPointer or IntPtr or ExplicitSpan => false,
                _ => throw ExceptionUtilities.UnexpectedValue(conversionKind),
            };
        }

        // Is the particular conversion a used-defined conversion?
        public static bool IsUserDefinedConversion(this ConversionKind conversionKind)
        {
            return conversionKind switch
            {
                ImplicitUserDefined or ExplicitUserDefined => true,
                _ => false,
            };
        }

        public static bool IsPointerConversion(this ConversionKind kind)
        {
            return kind switch
            {
                ImplicitPointerToVoid or ExplicitPointerToPointer or ExplicitPointerToInteger or ExplicitIntegerToPointer or ImplicitNullToPointer or ImplicitPointer => true,
                _ => false,
            };
        }
    }
}
