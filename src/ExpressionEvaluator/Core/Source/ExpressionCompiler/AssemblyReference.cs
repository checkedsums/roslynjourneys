// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Collections.ObjectModel;

using Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct AssemblyReaders(MetadataReader metadataReader, object symReader)
    {
        public readonly MetadataReader MetadataReader = metadataReader;
        public readonly object SymReader = symReader;
    }

    internal sealed class AssemblyReference(AssemblyIdentity identity) : IAssemblyReference
    {
        private readonly AssemblyIdentity _identity = identity;

        AssemblyIdentity IAssemblyReference.Identity => _identity;
        Version? IAssemblyReference.AssemblyVersionPattern => null;
        string INamedEntity.Name => _identity.Name;

        IAssemblyReference IModuleReference.GetContainingAssembly(EmitContext context) => this;

        IDefinition? IReference.AsDefinition(EmitContext context) => null;

        void IReference.Dispatch(MetadataVisitor visitor) => visitor.Visit(this);

        IEnumerable<ICustomAttribute> IReference.GetAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<ICustomAttribute>();

        Symbols.ISymbolInternal? IReference.GetInternalSymbol() => null;
    }

    internal abstract class CompileResult(byte[] assembly, string typeName, string methodName, ReadOnlyCollection<string>? formatSpecifiers)
    {
        internal readonly byte[] Assembly = assembly; // [] rather than ReadOnlyCollection<> to allow caller to create Stream easily
        internal readonly string TypeName = typeName;
        internal readonly string MethodName = methodName;
        internal readonly ReadOnlyCollection<string>? FormatSpecifiers = formatSpecifiers;

        public abstract Guid GetCustomTypeInfo(out ReadOnlyCollection<byte>? payload);
    }
}
