﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A type parameter for a synthesized class or method.
    /// </summary>
    internal sealed class SynthesizedSubstitutedTypeParameterSymbol(Symbol owner, TypeMap map, TypeParameterSymbol substitutedFrom, int ordinal) : SubstitutedTypeParameterSymbol(owner, map, substitutedFrom, ordinal)
    {
        public override bool IsImplicitlyDeclared => true;
        public override TypeParameterKind TypeParameterKind => ContainingSymbol is MethodSymbol ? TypeParameterKind.Method : TypeParameterKind.Type;

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            if (ContainingSymbol.Kind == SymbolKind.NamedType &&
                _underlyingTypeParameter.OriginalDefinition is SourceMethodTypeParameterSymbol definition &&
                ContainingSymbol.ContainingModule == definition.ContainingModule)
            {
                foreach (CSharpAttributeData attr in definition.GetAttributes())
                {
                    if (attr.AttributeClass is { HasCompilerLoweringPreserveAttribute: true })
                    {
                        AddSynthesizedAttribute(ref attributes, attr);
                    }
                }
            }

            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            if (this.HasUnmanagedTypeConstraint)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsUnmanagedAttribute(this));
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (ContainingSymbol is SynthesizedMethodBaseSymbol { InheritsBaseMethodAttributes: true })
            {
                return _underlyingTypeParameter.GetAttributes();
            }

            return ImmutableArray<CSharpAttributeData>.Empty;
        }
    }
}
