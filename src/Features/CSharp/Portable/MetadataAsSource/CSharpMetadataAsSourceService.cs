// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.MetadataAsSource;

using static SyntaxFactory;

internal sealed partial class CSharpMetadataAsSourceService : AbstractMetadataAsSourceService
{
    private static readonly AbstractFormattingRule s_memberSeparationRule = new FormattingRule();
    public static readonly CSharpMetadataAsSourceService Instance = new();

    private CSharpMetadataAsSourceService()
    {
    }

    protected override async Task<Document> AddAssemblyInfoRegionAsync(Document document, Compilation symbolCompilation, ISymbol symbol, CancellationToken cancellationToken)
    {
        var assemblyInfo = MetadataAsSourceHelpers.GetAssemblyInfo(symbol.ContainingAssembly);
        var assemblyPath = MetadataAsSourceHelpers.GetAssemblyDisplay(symbolCompilation, symbol.ContainingAssembly);

        var regionTrivia = RegionDirectiveTrivia(true)
            .WithTrailingTrivia(new[] { Space, PreprocessingMessage(assemblyInfo) });

        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = oldRoot.WithPrependedLeadingTrivia(
            Trivia(regionTrivia),
            CarriageReturnLineFeed,
            Comment("// " + assemblyPath),
            CarriageReturnLineFeed,
            Trivia(EndRegionDirectiveTrivia(true)),
            CarriageReturnLineFeed,
            CarriageReturnLineFeed);

        return document.WithSyntaxRoot(newRoot);
    }

    protected override ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document)
        => [s_memberSeparationRule, .. Formatter.GetDefaultFormattingRules(document)];

    protected override async Task<Document> ConvertDocCommentsToRegularCommentsAsync(Document document, IDocumentationCommentFormattingService docCommentFormattingService, CancellationToken cancellationToken)
    {
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var newSyntaxRoot = DocCommentConverter.ConvertToRegularComments(syntaxRoot, docCommentFormattingService, cancellationToken);

        return document.WithSyntaxRoot(newSyntaxRoot);
    }

    protected override ImmutableArray<AbstractReducer> GetReducers()
        => [
            new CSharpNameReducer(),
            new CSharpEscapingReducer(),
            new CSharpParenthesizedExpressionReducer(),
            new CSharpParenthesizedPatternReducer(),
            new CSharpDefaultExpressionReducer(),
        ];
}
