// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract class AbstractAddImportsService<TCompilationUnitSyntax, TNamespaceDeclarationSyntax, TUsingOrAliasSyntax, TExternSyntax>
    : IAddImportsService
    where TCompilationUnitSyntax : SyntaxNode
    where TNamespaceDeclarationSyntax : SyntaxNode
    where TUsingOrAliasSyntax : SyntaxNode
    where TExternSyntax : SyntaxNode
{
    protected AbstractAddImportsService()
    {
    }

    protected abstract string Language { get; }
    protected abstract SyntaxNode? GetAlias(TUsingOrAliasSyntax usingOrAlias);
    protected abstract ImmutableArray<SyntaxNode> GetGlobalImports(Compilation compilation, SyntaxGenerator generator);
    protected abstract SyntaxList<TUsingOrAliasSyntax> GetUsingsAndAliases(SyntaxNode node);
    protected abstract SyntaxList<TExternSyntax> GetExterns(SyntaxNode node);

    public AddImportPlacementOptions GetAddImportOptions(IOptionsReader configOptions, bool allowInHiddenRegions)
        => new()
        {
            PlaceSystemNamespaceFirst = configOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, Language),
            UsingDirectivePlacement = GetUsingDirectivePlacementCodeStyleOption(configOptions),
            AllowInHiddenRegions = allowInHiddenRegions
        };

    public abstract CodeStyleOption2<AddImportPlacement> GetUsingDirectivePlacementCodeStyleOption(IOptionsReader configOptions);

    private bool NotAlias(TUsingOrAliasSyntax usingOrAlias) => GetAlias(usingOrAlias) is null;
    private bool IsAlias(TUsingOrAliasSyntax usingOrAlias) => GetAlias(usingOrAlias) is not null;
    private bool HasAliases(SyntaxNode node) => GetUsingsAndAliases(node).Any(IsAlias);
    private bool HasUsings(SyntaxNode node) => GetUsingsAndAliases(node).Any(NotAlias);
    private bool HasExterns(SyntaxNode node) => GetExterns(node).Any();
    private bool HasAnyImports(SyntaxNode node) => GetUsingsAndAliases(node).Any() || GetExterns(node).Any();

    public bool HasExistingImport(
        Compilation compilation,
        SyntaxNode root,
        SyntaxNode? contextLocation,
        SyntaxNode import,
        SyntaxGenerator generator)
    {
        var globalImports = GetGlobalImports(compilation, generator);
        var containers = GetAllContainers(root, contextLocation);
        return HasExistingImport(import, containers, globalImports);
    }

    private static ImmutableArray<SyntaxNode> GetAllContainers(SyntaxNode root, SyntaxNode? contextLocation)
    {
        contextLocation ??= root;

        var applicableContainer = GetFirstApplicableContainer(contextLocation);
        return applicableContainer.GetAncestorsOrThis<SyntaxNode>().ToImmutableArray();
    }

    private bool HasExistingImport(
        SyntaxNode import, ImmutableArray<SyntaxNode> containers, ImmutableArray<SyntaxNode> globalImports)
    {
        foreach (var node in containers)
        {
            if (GetUsingsAndAliases(node).Any(u => IsEquivalentImport(u, import)))
            {
                return true;
            }

            if (GetExterns(node).Any(u => IsEquivalentImport(u, import)))
            {
                return true;
            }
        }

        foreach (var node in globalImports)
        {
            if (IsEquivalentImport(node, import))
            {
                return true;
            }
        }

        return false;
    }

    protected abstract bool IsEquivalentImport(SyntaxNode a, SyntaxNode b);

    public SyntaxNode GetImportContainer(SyntaxNode root, SyntaxNode? contextLocation, SyntaxNode import, AddImportPlacementOptions options)
    {
        contextLocation ??= root;
        GetContainers(root, contextLocation, options, out var externContainer, out var usingContainer, out var aliasContainer);

        switch (import)
        {
            case TExternSyntax:
                return externContainer;
            case TUsingOrAliasSyntax u:
                if (IsAlias(u))
                {
                    return aliasContainer;
                }

                return usingContainer;
        }

        throw new InvalidOperationException();
    }

    public SyntaxNode AddImports(
        Compilation compilation,
        SyntaxNode root,
        SyntaxNode? contextLocation,
        IEnumerable<SyntaxNode> newImports,
        SyntaxGenerator generator,
        AddImportPlacementOptions options,
        CancellationToken cancellationToken)
    {
        contextLocation ??= root;

        var globalImports = GetGlobalImports(compilation, generator);
        var containers = GetAllContainers(root, contextLocation);
        var filteredImports = newImports.Where(i => !HasExistingImport(i, containers, globalImports)).ToArray();

        var externAliases = filteredImports.OfType<TExternSyntax>().ToArray();
        var usingDirectives = filteredImports.OfType<TUsingOrAliasSyntax>().Where(NotAlias).ToArray();
        var aliasDirectives = filteredImports.OfType<TUsingOrAliasSyntax>().Where(IsAlias).ToArray();

        GetContainers(root, contextLocation, options,
            out var externContainer, out var usingContainer, out var aliasContainer);

        var newRoot = Rewrite(
            externAliases, usingDirectives, aliasDirectives,
            externContainer, usingContainer, aliasContainer,
            options, root, cancellationToken);

        return newRoot;
    }

    protected abstract SyntaxNode Rewrite(
        TExternSyntax[] externAliases, TUsingOrAliasSyntax[] usingDirectives, TUsingOrAliasSyntax[] aliasDirectives,
        SyntaxNode externContainer, SyntaxNode usingContainer, SyntaxNode aliasContainer,
        AddImportPlacementOptions options, SyntaxNode root, CancellationToken cancellationToken);

    private void GetContainers(SyntaxNode root, SyntaxNode contextLocation, AddImportPlacementOptions options, out SyntaxNode externContainer, out SyntaxNode usingContainer, out SyntaxNode aliasContainer)
    {
        var applicableContainer = GetFirstApplicableContainer(contextLocation);
        var contextSpine = applicableContainer.GetAncestorsOrThis<SyntaxNode>().ToImmutableArray();

        // The node we'll add to if we can't find a specific namespace with imports of 
        // the type we're trying to add.  This will be the closest namespace with any
        // imports in it
        var fallbackNode = contextSpine.FirstOrDefault(HasAnyImports);

        // If there aren't any existing imports then make sure we honour the inside namespace preference
        // for using directings if it's set
        if (fallbackNode is null && options.PlaceImportsInsideNamespaces)
            fallbackNode = contextSpine.OfType<TNamespaceDeclarationSyntax>().FirstOrDefault();

        // If all else fails use the root
        fallbackNode ??= root;

        // The specific container to add each type of import to.  We look for a container
        // that already has an import of the same type as the node we want to add to.
        // If we can find one, we add to that container.  If not, we call back to the 
        // innermost node with any imports.
        externContainer = contextSpine.FirstOrDefault(HasExterns) ?? fallbackNode;
        usingContainer = contextSpine.FirstOrDefault(HasUsings) ?? fallbackNode;
        aliasContainer = contextSpine.FirstOrDefault(HasAliases) ?? fallbackNode;
    }

    private static SyntaxNode? GetFirstApplicableContainer(SyntaxNode contextNode)
    {
        var usingDirective = contextNode.GetAncestor<TUsingOrAliasSyntax>();

        var node = usingDirective != null ? usingDirective.Parent! : contextNode;
        return node.GetAncestor<TNamespaceDeclarationSyntax>() ??
               (SyntaxNode?)node.GetAncestorOrThis<TCompilationUnitSyntax>();
    }
}
