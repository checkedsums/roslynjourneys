// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal abstract class AbstractDiagnosticPropertiesService : IDiagnosticPropertiesService
{
    public ImmutableDictionary<string, string> GetAdditionalProperties(Diagnostic diagnostic)
        => GetAdditionalProperties(diagnostic, GetCompilation());

    protected abstract Compilation GetCompilation();

    private static ImmutableDictionary<string, string> GetAdditionalProperties(
        Diagnostic diagnostic,
        Compilation compilation)
    {
        var assemblyIds = compilation.GetUnreferencedAssemblyIdentities(diagnostic);

        var result = ImmutableDictionary.CreateBuilder<string, string>();
        if (!assemblyIds.IsDefaultOrEmpty)
        {
            result.Add(
                DiagnosticPropertyConstants.UnreferencedAssemblyIdentity,
                assemblyIds[0].GetDisplayName());
        }

        return result.ToImmutable();
    }
}
