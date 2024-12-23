// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal readonly struct NamespaceOrTypeOrAliasSymbolWithAnnotations
        {
            private readonly TypeWithAnnotations _typeWithAnnotations;
            private readonly Symbol _symbol;

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations typeWithAnnotations)
            {
                Debug.Assert(typeWithAnnotations.HasType);
                _typeWithAnnotations = typeWithAnnotations;
                _symbol = null;
            }

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(Symbol symbol)
            {
                Debug.Assert(!(symbol is TypeSymbol));
                _typeWithAnnotations = default;
                _symbol = symbol;
            }

            internal TypeWithAnnotations TypeWithAnnotations => _typeWithAnnotations;
            internal Symbol Symbol => _symbol ?? TypeWithAnnotations.Type;
            internal bool IsType => !_typeWithAnnotations.IsDefault;
            internal bool IsAlias => _symbol?.Kind == SymbolKind.Alias;
            internal NamespaceOrTypeSymbol NamespaceOrTypeSymbol => Symbol as NamespaceOrTypeSymbol;
            internal bool IsDefault => !_typeWithAnnotations.HasType && _symbol is null;

            internal static NamespaceOrTypeOrAliasSymbolWithAnnotations CreateUnannotated(Symbol symbol)
            {
                if (symbol is null)
                {
                    return default;
                }
                return symbol is not TypeSymbol type ?
                    new NamespaceOrTypeOrAliasSymbolWithAnnotations(symbol) :
                    new NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations.Create(true, type));
            }

            public static implicit operator NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations typeWithAnnotations)
            {
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(typeWithAnnotations);
            }
        }
    }
}
