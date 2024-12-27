// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SymbolKindExtensions
    {
        public static LocalizableErrorArgument Localize(this SymbolKind kind)
        {
            return kind switch
            {
                SymbolKind.Namespace => MessageID.IDS_SK_NAMESPACE.Localize(),
                SymbolKind.NamedType => MessageID.IDS_SK_TYPE.Localize(),
                SymbolKind.TypeParameter => MessageID.IDS_SK_TYVAR.Localize(),
                SymbolKind.ArrayType => MessageID.IDS_SK_ARRAY.Localize(),
                SymbolKind.PointerType => MessageID.IDS_SK_POINTER.Localize(),
                SymbolKind.FunctionPointerType => MessageID.IDS_SK_FUNCTION_POINTER.Localize(),
                SymbolKind.DynamicType => MessageID.IDS_SK_DYNAMIC.Localize(),
                SymbolKind.Method => MessageID.IDS_SK_METHOD.Localize(),
                SymbolKind.Property => MessageID.IDS_SK_PROPERTY.Localize(),
                SymbolKind.Event => MessageID.IDS_SK_EVENT.Localize(),
                SymbolKind.Field => MessageID.IDS_SK_FIELD.Localize(),
                SymbolKind.Local or SymbolKind.Parameter or SymbolKind.RangeVariable => MessageID.IDS_SK_VARIABLE.Localize(),
                SymbolKind.Alias => MessageID.IDS_SK_ALIAS.Localize(),
                SymbolKind.Label => MessageID.IDS_SK_LABEL.Localize(),
                SymbolKind.Preprocessing => throw ExceptionUtilities.UnexpectedValue(kind),
                _ => MessageID.IDS_SK_UNKNOWN.Localize(),
            };
        }
    }
}
