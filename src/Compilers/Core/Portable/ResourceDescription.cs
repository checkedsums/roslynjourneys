// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Roslyn.Utilities;
using System.Reflection;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Representation of a resource whose contents are to be embedded in the output assembly.
    /// </summary>
    public sealed class ResourceDescription : Cci.IFileReference
    {
        internal readonly string ResourceName;
        internal readonly string? FileName; // null if embedded
        internal readonly bool IsPublic;
        internal readonly Func<Stream> DataProvider;
        private readonly CryptographicHashProvider _hashes;

        /// <summary>
        /// Creates a representation of a resource whose contents are to be embedded in the output assembly.
        /// </summary>
        /// <param name="resourceName">Resource name.</param>
        /// <param name="dataProvider">The callers will dispose the result after use.
        /// This allows the resources to be opened and read one at a time.
        /// </param>
        /// <param name="isPublic">True if the resource is public.</param>
        /// <remarks>
        /// Returns a stream of the data to embed.
        /// </remarks> 
        public ResourceDescription(string resourceName, Func<Stream> dataProvider, bool isPublic)
            : this(resourceName, fileName: null, dataProvider, isPublic, isEmbedded: true, checkArgs: true)
        {
        }

        /// <summary>
        /// Creates a representation of a resource whose file name will be recorded in the assembly.
        /// </summary>
        /// <param name="resourceName">Resource name.</param>
        /// <param name="fileName">File name with an extension to be stored in metadata.</param>
        /// <param name="dataProvider">The callers will dispose the result after use.
        /// This allows the resources to be opened and read one at a time.
        /// </param>
        /// <param name="isPublic">True if the resource is public.</param>
        /// <remarks>
        /// Function returning a stream of the resource content (used to calculate hash).
        /// </remarks>
        internal ResourceDescription(string resourceName, string? fileName, Func<Stream> dataProvider, bool isPublic, bool isEmbedded, bool checkArgs)
        {
            if (checkArgs)
            {
                if (dataProvider != null)
                    if (resourceName != null)
                        if (MetadataHelpers.IsValidMetadataIdentifier(resourceName))
                            if (!isEmbedded) ;
                            else if (fileName != null)
                                if (MetadataHelpers.IsValidMetadataFileName(fileName)) ;
                                else throw new ArgumentException(CodeAnalysisResources.EmptyOrInvalidFileName, nameof(fileName));
                            else throw new ArgumentNullException(nameof(fileName));
                        else throw new ArgumentException(CodeAnalysisResources.EmptyOrInvalidResourceName, nameof(resourceName));
                    else throw new ArgumentNullException(nameof(resourceName));
                else throw new ArgumentNullException(nameof(dataProvider));
            }

            this.ResourceName = resourceName;
            this.DataProvider = dataProvider;
            this.FileName = isEmbedded ? null : fileName;
            this.IsPublic = isPublic;
            _hashes = new ResourceHashProvider(this);
        }

        private sealed class ResourceHashProvider : CryptographicHashProvider
        {
            private readonly ResourceDescription _resource;

            public ResourceHashProvider(ResourceDescription resource)
            {
                RoslynDebug.Assert(resource != null);
                _resource = resource;
            }

            internal override ImmutableArray<byte> ComputeHash(HashAlgorithm algorithm)
            {
                try
                {
                    using var stream = _resource.DataProvider() ?? throw new InvalidOperationException(CodeAnalysisResources.ResourceDataProviderShouldReturnNonNullStream);
                    return [.. algorithm.ComputeHash(stream)];
                }
                catch (Exception ex)
                {
                    throw new ResourceException(_resource.FileName, ex);
                }
            }
        }

        internal bool IsEmbedded => FileName == null;

        internal Cci.ManagedResource ToManagedResource()
        {
            return new Cci.ManagedResource(ResourceName, IsPublic, IsEmbedded ? DataProvider : null, IsEmbedded ? null : this, offset: 0);
        }

        ImmutableArray<byte> Cci.IFileReference.GetHashValue(AssemblyHashAlgorithm algorithmId)
        {
            return _hashes.GetHash(algorithmId);
        }

        string? Cci.IFileReference.FileName => FileName;

        bool Cci.IFileReference.HasMetadata => false;
    }
}
