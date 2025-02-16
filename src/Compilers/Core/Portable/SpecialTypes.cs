﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class SpecialTypes
    {
        /// <summary> Array of names for types from Cor Library.
        /// The names should correspond to ids from TypeId enum so
        /// that we could use ids to index into the array </summary>
        private static readonly string?[] s_emittedNames =
        [   // The following things should be in sync:
            // 1) SpecialType/InternalSpecialType enum
            // 2) names in SpecialTypes.EmittedNames array.
            // 3) languageNames in SemanticFacts.cs
            // 4) languageNames in SemanticFacts.vb
            null, // SpecialType.None
            "System.Object",
            "System.Enum",
            "System.MulticastDelegate",
            "System.Delegate",
            "System.ValueType",
            "System.Void",
            "System.Boolean",
            "System.Char",
            "System.SByte",
            "System.Byte",
            "System.Int16",
            "System.UInt16",
            "System.Int32",
            "System.UInt32",
            "System.Int64",
            "System.UInt64",
            "System.Decimal",
            "System.Single",
            "System.Double",
            "System.String",
            "System.IntPtr",
            "System.UIntPtr",
            "System.Array",
            "System.Collections.IEnumerable",
            "System.Collections.Generic.IEnumerable`1",
            "System.Collections.Generic.IList`1",
            "System.Collections.Generic.ICollection`1",
            "System.Collections.IEnumerator",
            "System.Collections.Generic.IEnumerator`1",
            "System.Collections.Generic.IReadOnlyList`1",
            "System.Collections.Generic.IReadOnlyCollection`1",
            "System.Nullable`1",
            "System.DateTime",
            "System.Runtime.CompilerServices.IsVolatile",
            "System.IDisposable",
            "System.TypedReference",
            "System.ArgIterator",
            "System.RuntimeArgumentHandle",
            "System.RuntimeFieldHandle",
            "System.RuntimeMethodHandle",
            "System.RuntimeTypeHandle",
            "System.IAsyncResult",
            "System.AsyncCallback",
            "System.Runtime.CompilerServices.RuntimeFeature",
            "System.Runtime.CompilerServices.PreserveBaseOverridesAttribute",
            "System.Runtime.CompilerServices.InlineArrayAttribute",
            "System.ReadOnlySpan`1",
            "System.IFormatProvider",
            "System.Type",
            "System.Reflection.MethodBase",
            "System.Reflection.MethodInfo",
        ];

        private static readonly Dictionary<string, ExtendedSpecialType> s_nameToTypeIdMap;

        private static readonly Cci.PrimitiveTypeCode[] s_typeIdToTypeCodeMap;
        private static readonly SpecialType[] s_typeCodeToTypeIdMap;

        static SpecialTypes()
        {
            s_nameToTypeIdMap = new Dictionary<string, ExtendedSpecialType>((int)InternalSpecialType.NextAvailable - 1);

            int i;

            for (i = 1; i < s_emittedNames.Length; i++)
            {
                string? name = s_emittedNames[i];
                RoslynDebug.Assert(name is not null);
                Debug.Assert(name.IndexOf('+') < 0); // Compilers aren't prepared to lookup for a nested special type.
                s_nameToTypeIdMap.Add(name, (ExtendedSpecialType)i);
            }

            s_typeIdToTypeCodeMap = new Cci.PrimitiveTypeCode[(int)SpecialType.Count + 1];

            for (i = 0; i < s_typeIdToTypeCodeMap.Length; i++)
            {
                s_typeIdToTypeCodeMap[i] = Cci.PrimitiveTypeCode.NotPrimitive;
            }

            s_typeIdToTypeCodeMap[(int)SpecialType.System_Boolean] = Cci.PrimitiveTypeCode.Boolean;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_Char] = Cci.PrimitiveTypeCode.Char;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_Void] = Cci.PrimitiveTypeCode.Void;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_String] = Cci.PrimitiveTypeCode.String;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_Int64] = Cci.PrimitiveTypeCode.Int64;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_Int32] = Cci.PrimitiveTypeCode.Int32;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_Int16] = Cci.PrimitiveTypeCode.Int16;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_SByte] = Cci.PrimitiveTypeCode.Int8;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_UInt64] = Cci.PrimitiveTypeCode.UInt64;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_UInt32] = Cci.PrimitiveTypeCode.UInt32;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_UInt16] = Cci.PrimitiveTypeCode.UInt16;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_Byte] = Cci.PrimitiveTypeCode.UInt8;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_Single] = Cci.PrimitiveTypeCode.Float32;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_Double] = Cci.PrimitiveTypeCode.Float64;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_IntPtr] = Cci.PrimitiveTypeCode.IntPtr;
            s_typeIdToTypeCodeMap[(int)SpecialType.System_UIntPtr] = Cci.PrimitiveTypeCode.UIntPtr;

            s_typeCodeToTypeIdMap = new SpecialType[(int)Cci.PrimitiveTypeCode.Invalid + 1];

            for (i = 0; i < s_typeCodeToTypeIdMap.Length; i++)
            {
                s_typeCodeToTypeIdMap[i] = SpecialType.None;
            }

            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Boolean] = SpecialType.System_Boolean;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Char] = SpecialType.System_Char;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Void] = SpecialType.System_Void;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.String] = SpecialType.System_String;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Int64] = SpecialType.System_Int64;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Int32] = SpecialType.System_Int32;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Int16] = SpecialType.System_Int16;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Int8] = SpecialType.System_SByte;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.UInt64] = SpecialType.System_UInt64;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.UInt32] = SpecialType.System_UInt32;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.UInt16] = SpecialType.System_UInt16;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.UInt8] = SpecialType.System_Byte;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Float32] = SpecialType.System_Single;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.Float64] = SpecialType.System_Double;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.IntPtr] = SpecialType.System_IntPtr;
            s_typeCodeToTypeIdMap[(int)Cci.PrimitiveTypeCode.UIntPtr] = SpecialType.System_UIntPtr;
        }

        /// <summary>
        /// Gets the name of the special type as it would appear in metadata.
        /// </summary>
        public static string? GetMetadataName(this ExtendedSpecialType id) => s_emittedNames[(int)id];

        public static ExtendedSpecialType GetTypeFromMetadataName(string metadataName) =>
            s_nameToTypeIdMap.TryGetValue(metadataName, out ExtendedSpecialType id)
                        ? id : default;

        public static SpecialType GetTypeFromMetadataName(Cci.PrimitiveTypeCode typeCode) => s_typeCodeToTypeIdMap[(int)typeCode];

        public static Cci.PrimitiveTypeCode GetTypeCode(SpecialType typeId) => s_typeIdToTypeCodeMap[(int)typeId];
    }
}
