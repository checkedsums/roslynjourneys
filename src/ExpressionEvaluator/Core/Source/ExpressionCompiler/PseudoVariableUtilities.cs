// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Clr;
using Roslyn.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct Alias
    {
        internal readonly DkmClrAliasKind Kind;
        internal readonly string Name;
        internal readonly string FullName;
        internal readonly string Type;
        internal readonly Guid CustomTypeInfoId;
        internal readonly ReadOnlyCollection<byte> CustomTypeInfo;

        internal Alias(DkmClrAliasKind kind, string name, string fullName, string type, Guid customTypeInfoId, ReadOnlyCollection<byte> customTypeInfo)
        {
            RoslynDebug.Assert(!string.IsNullOrEmpty(fullName));
            RoslynDebug.Assert(!string.IsNullOrEmpty(type));

            Kind = kind;
            Name = name;
            FullName = fullName;
            Type = type;
            CustomTypeInfoId = customTypeInfoId;
            CustomTypeInfo = customTypeInfo;
        }
    }

    internal static class PseudoVariableUtilities
    {
        private const int ReturnValuePrefixLength = 12; // "$ReturnValue"

        internal static bool TryParseReturnValueIndex(string name, out int index)
        {
            Debug.Assert(name.StartsWith("$ReturnValue", StringComparison.OrdinalIgnoreCase));
            int n = name.Length;
            index = 0;
            return n == ReturnValuePrefixLength ||
                (n > ReturnValuePrefixLength) && int.TryParse(name.Substring(ReturnValuePrefixLength), NumberStyles.None, CultureInfo.InvariantCulture, out index);
        }

        internal static bool IsReturnValueWithoutIndex(this Alias alias)
        {
            Debug.Assert(alias.Kind != DkmClrAliasKind.ReturnValue ||
                alias.FullName.StartsWith("$ReturnValue", StringComparison.OrdinalIgnoreCase));
            return
                alias.Kind == DkmClrAliasKind.ReturnValue &&
                alias.FullName.Length == ReturnValuePrefixLength;
        }
    }
}
