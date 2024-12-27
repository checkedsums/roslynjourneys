// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class ConstructedMethodSymbol : SubstitutedMethodSymbol
    {
        private readonly ImmutableArray<TypeWithAnnotations> _typeArgumentsWithAnnotations;

        internal ConstructedMethodSymbol(MethodSymbol constructedFrom, ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations)
            : base(containingSymbol: constructedFrom.ContainingSymbol,
                   map: new TypeMap(constructedFrom.ContainingType, constructedFrom.OriginalDefinition.TypeParameters, typeArgumentsWithAnnotations),
                   originalDefinition: constructedFrom.OriginalDefinition,
                   constructedFrom: constructedFrom)
        {
            _typeArgumentsWithAnnotations = typeArgumentsWithAnnotations;
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => _typeArgumentsWithAnnotations;
    }
}
