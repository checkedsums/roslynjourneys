// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class SpecialTypeExtensions
    {
        public static bool CanBeConst(this SpecialType specialType)
        {
            return specialType switch
            {
                SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Byte or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal or SpecialType.System_String => true,
                _ => false,
            };
        }

        public static bool IsValidVolatileFieldType(this SpecialType specialType)
        {
            return specialType switch
            {
                SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Char or SpecialType.System_Single or SpecialType.System_Boolean or SpecialType.System_IntPtr or SpecialType.System_UIntPtr => true,
                _ => false,
            };
        }

        public static int FixedBufferElementSizeInBytes(this SpecialType specialType)
        {
            // SizeInBytes() handles decimal (contrary to the language spec).  But decimal is not allowed
            // as a fixed buffer element type.
            return specialType == SpecialType.System_Decimal ? 0 : specialType.SizeInBytes();
        }
    }
}
