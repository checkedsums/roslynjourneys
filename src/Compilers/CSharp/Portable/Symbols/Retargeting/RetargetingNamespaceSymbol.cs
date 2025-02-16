﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a namespace of a RetargetingModuleSymbol. Essentially this is a wrapper around 
    /// another NamespaceSymbol that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal sealed class RetargetingNamespaceSymbol
        : NamespaceSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// The underlying NamespaceSymbol, cannot be another RetargetingNamespaceSymbol.
        /// </summary>
        private readonly NamespaceSymbol _underlyingNamespace;

        public RetargetingNamespaceSymbol(RetargetingModuleSymbol retargetingModule, NamespaceSymbol underlyingNamespace)
        {
            Debug.Assert(retargetingModule is not null);
            Debug.Assert(underlyingNamespace is not null);
            Debug.Assert(!(underlyingNamespace is RetargetingNamespaceSymbol));

            _retargetingModule = retargetingModule;
            _underlyingNamespace = underlyingNamespace;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public NamespaceSymbol UnderlyingNamespace
        {
            get
            {
                return _underlyingNamespace;
            }
        }

        internal override NamespaceExtent Extent
        {
            get
            {
                return new NamespaceExtent(_retargetingModule);
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return RetargetMembers(_underlyingNamespace.GetMembers());
        }

        private ImmutableArray<Symbol> RetargetMembers(ImmutableArray<Symbol> underlyingMembers)
        {
            var builder = ArrayBuilder<Symbol>.GetInstance(underlyingMembers.Length);

            foreach (Symbol s in underlyingMembers)
            {
                // Skip explicitly declared local types.
                if (s.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)s).IsExplicitDefinitionOfNoPiaLocalType)
                {
                    continue;
                }

                builder.Add(this.RetargetingTranslator.Retarget(s));
            }

            return builder.ToImmutableAndFree();
        }

        internal override ImmutableArray<Symbol> GetMembersUnordered()
        {
            return RetargetMembers(_underlyingNamespace.GetMembersUnordered());
        }

        public override ImmutableArray<Symbol> GetMembers(ReadOnlyMemory<char> name)
        {
            return RetargetMembers(_underlyingNamespace.GetMembers(name));
        }

        internal override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return RetargetTypeMembers(_underlyingNamespace.GetTypeMembersUnordered());
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return RetargetTypeMembers(_underlyingNamespace.GetTypeMembers());
        }

        private ImmutableArray<NamedTypeSymbol> RetargetTypeMembers(ImmutableArray<NamedTypeSymbol> underlyingMembers)
        {
            var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance(underlyingMembers.Length);

            foreach (NamedTypeSymbol t in underlyingMembers)
            {
                // Skip explicitly declared local types.
                if (t.IsExplicitDefinitionOfNoPiaLocalType)
                {
                    continue;
                }

                Debug.Assert(t.PrimitiveTypeCode == Cci.PrimitiveTypeCode.NotPrimitive);
                builder.Add(this.RetargetingTranslator.Retarget(t, RetargetOptions.RetargetPrimitiveTypesByName));
            }

            return builder.ToImmutableAndFree();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            return RetargetTypeMembers(_underlyingNamespace.GetTypeMembers(name));
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return RetargetTypeMembers(_underlyingNamespace.GetTypeMembers(name, arity));
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingNamespace.ContainingSymbol);
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _retargetingModule.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingNamespace.DeclaringSyntaxReferences;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        public override bool IsGlobalNamespace
        {
            get
            {
                return _underlyingNamespace.IsGlobalNamespace;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingNamespace.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
        {
            return _underlyingNamespace.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

#nullable enable

        internal override NamedTypeSymbol? LookupMetadataType(ref MetadataTypeName typeName)
        {
            // This method is invoked when looking up a type by metadata type
            // name through a RetargetingAssemblySymbol. For instance, in
            // UnitTests.Symbols.Metadata.PE.NoPia.LocalTypeSubstitution2.
            NamedTypeSymbol? underlying = _underlyingNamespace.LookupMetadataType(ref typeName);

            if (underlying is null)
            {
                return null;
            }

            Debug.Assert((object)underlying.ContainingModule == (object)_retargetingModule.UnderlyingModule);
            Debug.Assert(!underlying.IsErrorType());

            if (underlying.IsExplicitDefinitionOfNoPiaLocalType)
            {
                // Explicitly defined local types should be hidden.
                return null;
            }

            return this.RetargetingTranslator.Retarget(underlying, RetargetOptions.RetargetPrimitiveTypesByName);
        }

#nullable disable

        internal override void GetExtensionMethods(ArrayBuilder<MethodSymbol> methods, string nameOpt, int arity, LookupOptions options)
        {
            var underlyingMethods = ArrayBuilder<MethodSymbol>.GetInstance();
            _underlyingNamespace.GetExtensionMethods(underlyingMethods, nameOpt, arity, options);
            foreach (var underlyingMethod in underlyingMethods)
            {
                methods.Add(this.RetargetingTranslator.Retarget(underlyingMethod));
            }
            underlyingMethods.Free();
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
