// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.F1Help;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.F1Help;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.F1Help)]
public sealed class F1HelpTests
{
    private static async Task TestAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string expectedText)
    {
        using var workspace = TestWorkspace.CreateCSharp(markup, composition: VisualStudioTestCompositions.LanguageServices);
        var caret = workspace.Documents.First().CursorPosition;

        var service = Assert.IsType<CSharpHelpContextService>(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<IHelpContextService>());
        var actualText = await service.GetHelpTermAsync(workspace.CurrentSolution.Projects.First().Documents.First(), workspace.Documents.First().SelectedSpans.First(), CancellationToken.None);
        Assert.Equal(expectedText, actualText);
    }

    private static async Task Test_KeywordAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string expectedText)
    {
        await TestAsync(markup, expectedText + "_CSharpKeyword");
    }

    [Fact]
    public async Task TestInternal()
    {
        await Test_KeywordAsync(
            """
            intern[||]al class C
            {
            }
            """, "internal");
    }

    [Fact]
    public async Task TestProtected()
    {
        await Test_KeywordAsync(
            """
            public class C
            {
                protec[||]ted void goo();
            }
            """, "protected");
    }

    [Fact]
    public async Task TestProtectedInternal1()
    {
        await Test_KeywordAsync(
            """
            public class C
            {
                internal protec[||]ted void goo();
            }
            """, "protectedinternal");
    }

    [Fact]
    public async Task TestProtectedInternal2()
    {
        await Test_KeywordAsync(
            """
            public class C
            {
                protec[||]ted internal void goo();
            }
            """, "protectedinternal");
    }

    [Fact]
    public async Task TestPrivateProtected1()
    {
        await Test_KeywordAsync(
            """
            public class C
            {
                private protec[||]ted void goo();
            }
            """, "privateprotected");
    }

    [Fact]
    public async Task TestPrivateProtected2()
    {
        await Test_KeywordAsync(
            """
            public class C
            {
                priv[||]ate protected void goo();
            }
            """, "privateprotected");
    }

    [Fact]
    public async Task TestPrivateProtected3()
    {
        await Test_KeywordAsync(
            """
            public class C
            {
                protected priv[||]ate void goo();
            }
            """, "privateprotected");
    }

    [Fact]
    public async Task TestPrivateProtected4()
    {
        await Test_KeywordAsync(
            """
            public class C
            {
                prot[||]ected private void goo();
            }
            """, "privateprotected");
    }

    [Fact]
    public async Task TestModifierSoup()
    {
        await Test_KeywordAsync(
"""
public class C
{
    private new prot[||]ected static unsafe void foo()
    {
    }
}
""", "privateprotected");
    }

    [Fact]
    public async Task TestModifierSoupField()
    {
        await Test_KeywordAsync(
"""
public class C
{
    new prot[||]ected static unsafe private goo;
}
""", "privateprotected");
    }

    [Fact]
    public async Task TestVoid()
    {
        await Test_KeywordAsync(
            """
            class C
            {
                vo[||]id goo()
                {
                }
                """, "required");
        }

        [Fact]
        public async Task TestScoped()
        {
            await Test_KeywordAsync("""
                sc[||]oped var r = new R();
                ref struct R
                {
                }
                """, "scoped");
        }
    }
}
