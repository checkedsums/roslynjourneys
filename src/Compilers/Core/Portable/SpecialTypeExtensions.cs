// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable format

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class SpecialTypeExtensions
    {
        /// <summary> Checks if a type is considered a "built-in integral" by CLR. </summary>
        public static bool IsClrInteger(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_IntPtr or SpecialType.System_UIntPtr => true,
            _ => false };
        /// <summary> Checks if a type is a primitive of a fixed size. </summary>
        public static bool IsBlittable(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double => true,
            _ => false };

        public static bool IsValueType(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_Void or SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal or SpecialType.System_IntPtr or SpecialType.System_UIntPtr or SpecialType.System_Nullable_T or SpecialType.System_DateTime or SpecialType.System_TypedReference or SpecialType.System_ArgIterator or SpecialType.System_RuntimeArgumentHandle or SpecialType.System_RuntimeFieldHandle or SpecialType.System_RuntimeMethodHandle or SpecialType.System_RuntimeTypeHandle => true,
            _ => false };

        public static int SizeInBytes(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_SByte => sizeof(sbyte),
            SpecialType.System_Byte => sizeof(byte),
            SpecialType.System_Int16 => sizeof(short),
            SpecialType.System_UInt16 => sizeof(ushort),
            SpecialType.System_Int32 => sizeof(int),
            SpecialType.System_UInt32 => sizeof(uint),
            SpecialType.System_Int64 => sizeof(long),
            SpecialType.System_UInt64 => sizeof(ulong),
            SpecialType.System_Char => sizeof(char),
            SpecialType.System_Single => sizeof(float),
            SpecialType.System_Double => sizeof(double),
            SpecialType.System_Boolean => sizeof(bool),
            SpecialType.System_Decimal => sizeof(decimal),//This isn't in the spec, but it is handled by dev10
            _ => 0,// invalid
        };

        ///  <summary> These special types are structs that contain fields of the same type
        ///            (e.g. System.Int32 contains a field of type System.Int32).</summary>
        public static bool IsPrimitiveRecursiveStruct(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_Char or SpecialType.System_Double or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64 or SpecialType.System_IntPtr or SpecialType.System_UIntPtr or SpecialType.System_SByte or SpecialType.System_Single => true,
            _ => false };

        public static bool IsValidEnumUnderlyingType(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 => true,
            _ => false };

        public static bool IsNumericType(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal => true,
            _ => false };

        /// <summary> Checks if a type is considered a "built-in integral" by CLR. </summary>
        public static bool IsIntegralType(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 => true,
            _ => false };

        public static bool IsUnsignedIntegralType(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_Byte or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64 => true,
            _ => false };

        public static bool IsSignedIntegralType(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64 => true,
            _ => false };

        /// <summary>
        /// For signed integer types return number of bits for their representation minus 1. 
        /// I.e. 7 for Int8, 31 for Int32, etc.
        /// Used for checking loop end condition for VB for loop.
        /// </summary>
        public static int VBForToShiftBits(this SpecialType specialType) => specialType switch
        {
            SpecialType.System_SByte => 7,
            SpecialType.System_Int16 => 15,
            SpecialType.System_Int32 => 31,
            SpecialType.System_Int64 => 63,
            _ => throw ExceptionUtilities.UnexpectedValue(specialType),
        };

        public static SpecialType FromRuntimeTypeOfLiteralValue(object value) => value switch
        {
            string => SpecialType.System_String,
            char => SpecialType.System_Char,
            decimal => SpecialType.System_Decimal,
            double => SpecialType.System_Double,
            float => SpecialType.System_Single,
            ulong => SpecialType.System_UInt64,
            long => SpecialType.System_Int64,
            uint => SpecialType.System_UInt32,
            int => SpecialType.System_Int32,
            ushort => SpecialType.System_UInt16,
            short => SpecialType.System_Int16,
            byte => SpecialType.System_Byte,
            sbyte => SpecialType.System_SByte,
            bool => SpecialType.System_Boolean,
            DateTime => SpecialType.System_DateTime,
            _ => SpecialType.None,
        };

        /// <summary>
        /// Tells whether a different code path can be taken based on the fact, that a given type is a special type.
        /// This method is called in places where conditions like <c>specialType != SpecialType.None</c> were previously used.
        /// The main reason for this method to exist is to prevent such conditions, which introduce silent code changes every time a new special type is added.
        /// This doesn't mean the checked special type range of this method cannot be modified,
        /// but rather that each usage of this method needs to be reviewed to make sure everything works as expected in such cases
        /// </summary>
        public static bool CanOptimizeBehavior(this SpecialType specialType)
            => specialType is >= SpecialType.System_Object and <= SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute;

        /// <summary>
        /// Convert a boxed primitive (generally of the backing type of an enum) into a ulong.
        /// </summary>
        internal static ulong ConvertUnderlyingValueToUInt64(this SpecialType enumUnderlyingType, object value) => unchecked(enumUnderlyingType switch
        {
            SpecialType.System_SByte => (ulong)(sbyte)value,
            SpecialType.System_Int16 => (ulong)(short)value,
            SpecialType.System_Int32 => (ulong)(int)value,
            SpecialType.System_Int64 => (ulong)(long)value,
            SpecialType.System_Byte => (byte)value,
            SpecialType.System_UInt16 => (ushort)value,
            SpecialType.System_UInt32 => (uint)value,
            SpecialType.System_UInt64 => (ulong)value,
            _ => throw ExceptionUtilities.UnexpectedValue(enumUnderlyingType),
        });
    }
}
