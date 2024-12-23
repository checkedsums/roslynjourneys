// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public static class TestOptions
    {
        public static readonly CSharpParseOptions Regular = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);
        public static readonly CSharpParseOptions Script = Regular.WithKind(SourceCodeKind.Script);
        public static readonly CSharpParseOptions RegularDefault = Regular.WithLanguageVersion(LanguageVersion.Default);

        /// <summary>
        /// Usages of <see cref="TestOptions.RegularNext"/> and <see cref="LanguageVersionFacts.CSharpNext"/>
        /// will be replaced with TestOptions.RegularN and LanguageVersion.CSharpN when language version N is introduced.
        /// </summary>
        public static readonly CSharpParseOptions RegularNext = Regular.WithLanguageVersion(LanguageVersion.Preview);

        public static readonly CSharpParseOptions RegularPreview = Regular.WithLanguageVersion(LanguageVersion.Preview);
        public static readonly CSharpParseOptions RegularWithDocumentationComments = Regular.WithDocumentationMode(DocumentationMode.Diagnose);
        public static readonly CSharpParseOptions RegularPreviewWithDocumentationComments = RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose);
        public static readonly CSharpParseOptions RegularWithLegacyStrongName = Regular;
        public static readonly CSharpParseOptions WithoutImprovedOverloadCandidates = Regular;
        public static readonly CSharpParseOptions WithCovariantReturns = Regular;
        public static readonly CSharpParseOptions WithoutCovariantReturns = Regular;

        public static readonly CSharpParseOptions RegularWithExtendedPartialMethods = RegularPreview;
        public static readonly CSharpParseOptions RegularWithFileScopedNamespaces = Regular;

        private static readonly SmallDictionary<string, string> s_experimentalFeatures = new SmallDictionary<string, string> { };
        public static readonly CSharpParseOptions ExperimentalParseOptions =
            new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.None, languageVersion: LanguageVersion.Preview).WithFeatures(s_experimentalFeatures);

        // Enable pattern-switch translation even for switches that use no new syntax. This is used
        // to help ensure compatibility of the semantics of the new switch binder with the old switch
        // binder, so that we may eliminate the old one in the future.
        public static readonly CSharpParseOptions Regular6WithV7SwitchBinder = Regular.WithFeatures(new Dictionary<string, string>() { { "testV7SwitchBinder", "true" } });

        public static readonly CSharpParseOptions RegularWithoutRecursivePatterns = Regular;
        public static readonly CSharpParseOptions RegularWithRecursivePatterns = Regular;
        public static readonly CSharpParseOptions RegularWithoutPatternCombinators = Regular;
        public static readonly CSharpParseOptions RegularWithPatternCombinators = Regular;
        public static readonly CSharpParseOptions RegularWithExtendedPropertyPatterns = RegularPreview;
        public static readonly CSharpParseOptions RegularWithListPatterns = RegularPreview;

        public static readonly CSharpCompilationOptions ReleaseDll = CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions ReleaseExe = CreateTestOptions(OutputKind.ConsoleApplication, OptimizationLevel.Release);

        public static readonly CSharpCompilationOptions ReleaseDebugDll = ReleaseDll.WithDebugPlusMode(true);

        public static readonly CSharpCompilationOptions ReleaseDebugExe = ReleaseExe.WithDebugPlusMode(true);

        public static readonly CSharpCompilationOptions DebugDll = CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, OptimizationLevel.Debug);
        public static readonly CSharpCompilationOptions DebugExe = CreateTestOptions(OutputKind.ConsoleApplication, OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions DebugDllThrowing = DebugDll.WithMetadataReferenceResolver(new ThrowingMetadataReferenceResolver());
        public static readonly CSharpCompilationOptions DebugExeThrowing = DebugExe.WithMetadataReferenceResolver(new ThrowingMetadataReferenceResolver());

        public static readonly CSharpCompilationOptions ReleaseWinMD = CreateTestOptions(OutputKind.WindowsRuntimeMetadata, OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions DebugWinMD = CreateTestOptions(OutputKind.WindowsRuntimeMetadata, OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions ReleaseModule = CreateTestOptions(OutputKind.NetModule, OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions DebugModule = CreateTestOptions(OutputKind.NetModule, OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions UnsafeReleaseDll = ReleaseDll.WithAllowUnsafe(true);
        public static readonly CSharpCompilationOptions UnsafeReleaseExe = ReleaseExe.WithAllowUnsafe(true);

        public static readonly CSharpCompilationOptions UnsafeDebugDll = DebugDll.WithAllowUnsafe(true);
        public static readonly CSharpCompilationOptions UnsafeDebugExe = DebugExe.WithAllowUnsafe(true);

        public static readonly CSharpCompilationOptions SigningReleaseDll = ReleaseDll.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningReleaseExe = ReleaseExe.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningReleaseModule = ReleaseModule.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningDebugDll = DebugDll.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);

        public static readonly EmitOptions NativePdbEmit = EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Pdb);

        public static readonly GeneratorDriverOptions GeneratorDriverOptions = new GeneratorDriverOptions(trackIncrementalGeneratorSteps: true, baseDirectory: TempRoot.Root);

        /// <summary>
        /// Create <see cref="CSharpCompilationOptions"/> with the maximum warning level.
        /// </summary>
        /// <param name="outputKind">The output kind of the created compilation options.</param>
        /// <param name="optimizationLevel">The optimization level of the created compilation options.</param>
        /// <param name="allowUnsafe">A boolean specifying whether to allow unsafe code. Defaults to false.</param>
        /// <returns>A CSharpCompilationOptions with the specified <paramref name="outputKind"/>, <paramref name="optimizationLevel"/>, and <paramref name="allowUnsafe"/>.</returns>
        internal static CSharpCompilationOptions CreateTestOptions(OutputKind outputKind, OptimizationLevel optimizationLevel, bool allowUnsafe = false)
            => new CSharpCompilationOptions(outputKind, optimizationLevel: optimizationLevel, warningLevel: Diagnostic.MaxWarningLevel, allowUnsafe: allowUnsafe);
    }
}
