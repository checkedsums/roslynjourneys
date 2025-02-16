﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class AssemblyReference : Cci.IAssemblyReference
    {
        // assembly symbol that represents the target assembly:
        private readonly AssemblySymbol _targetAssembly;

        internal AssemblyReference(AssemblySymbol assemblySymbol)
        {
            Debug.Assert(assemblySymbol is not null);
            _targetAssembly = assemblySymbol;
        }

        public AssemblyIdentity Identity => _targetAssembly.Identity;
        public Version AssemblyVersionPattern => _targetAssembly.AssemblyVersionPattern;

        public override string ToString()
        {
            return _targetAssembly.ToString();
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        string Cci.INamedEntity.Name => Identity.Name;

        Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(CodeAnalysis.Emit.EmitContext context)
        {
            return this;
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(CodeAnalysis.Emit.EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        Cci.IDefinition Cci.IReference.AsDefinition(CodeAnalysis.Emit.EmitContext context)
        {
            return null;
        }

        CodeAnalysis.Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;
    }
}
