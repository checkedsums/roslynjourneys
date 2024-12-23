// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Specifies the language version.
    /// </summary>
    public enum LanguageVersion
    {
        CSharp13 = 1300,

        LatestMajor = int.MaxValue - 2,

        Preview = int.MaxValue - 1,

        Latest = int.MaxValue,

        Default = 0,
    }

    internal static class LanguageVersionExtensionsInternal
    {
        internal static bool IsValid(this LanguageVersion value)
            => value switch
            {
                LanguageVersion.CSharp13 or LanguageVersion.Preview => true,
                _ => false,
            };
    }

    public static class LanguageVersionFacts
    {
        internal const LanguageVersion CSharpNext = LanguageVersion.Preview;

        public static string ToDisplayString(this LanguageVersion version)
        {
            return version switch
            {
                LanguageVersion.CSharp13 => "13.0",
                LanguageVersion.Default => "default",
                LanguageVersion.Latest => "latest",
                LanguageVersion.LatestMajor => "latestmajor",
                LanguageVersion.Preview => "preview",
                _ => throw ExceptionUtilities.UnexpectedValue(version),
            };
        }

        public static bool TryParse(string? version, out LanguageVersion result)
        {
            return (result = CaseInsensitiveComparison.ToLower(version) switch
            {
                "default" => LanguageVersion.Default,
                "latest" => LanguageVersion.Latest,
                "latestmajor" => LanguageVersion.LatestMajor,
                "preview" => LanguageVersion.Preview,
                "13" or "13.0" => LanguageVersion.CSharp13,
                null or _ => LanguageVersion.Default,
            }) != LanguageVersion.Default || version == null;
        }

        public static LanguageVersion MapSpecifiedToEffectiveVersion(this LanguageVersion version)
        {
            return version switch
            {
                LanguageVersion.Latest or LanguageVersion.Default or LanguageVersion.LatestMajor => LanguageVersion.CSharp13,
                _ => version,
            };
        }

        internal static LanguageVersion CurrentVersion => LanguageVersion.CSharp13;
    }
}
