// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class PrimitiveTypeCodeExtensions
    {
        public static bool Is64BitIntegral(this Cci.PrimitiveTypeCode kind) => kind switch
        {
            Cci.PrimitiveTypeCode.Int64 or Cci.PrimitiveTypeCode.UInt64 => true,
            _ => false,
        };

        public static bool IsUnsigned(this Cci.PrimitiveTypeCode kind) => kind switch
        {
            Cci.PrimitiveTypeCode.UInt8 or Cci.PrimitiveTypeCode.UInt16 or Cci.PrimitiveTypeCode.UInt32 or Cci.PrimitiveTypeCode.UInt64 or Cci.PrimitiveTypeCode.UIntPtr or Cci.PrimitiveTypeCode.Char or Cci.PrimitiveTypeCode.Pointer or Cci.PrimitiveTypeCode.FunctionPointer => true,
            _ => false,
        };

        public static bool IsFloatingPoint(this Cci.PrimitiveTypeCode kind) => kind switch
        {
            Cci.PrimitiveTypeCode.Float32 or Cci.PrimitiveTypeCode.Float64 => true,
            _ => false,
        };
    }
}
