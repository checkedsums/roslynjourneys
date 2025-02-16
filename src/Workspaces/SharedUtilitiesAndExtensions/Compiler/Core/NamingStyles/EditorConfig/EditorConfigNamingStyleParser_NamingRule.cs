﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal static partial class EditorConfigNamingStyleParser
{
    private static bool TryGetSerializableNamingRule(
        string namingRuleTitle,
        SymbolSpecification symbolSpec,
        NamingStyle namingStyle,
        IReadOnlyDictionary<string, string> conventionsDictionary,
        [NotNullWhen(true)] out SerializableNamingRule? serializableNamingRule,
        out int priority)
    {
        priority = GetRulePriority(namingRuleTitle, conventionsDictionary);

        if (!TryGetRuleSeverity(namingRuleTitle, conventionsDictionary, out var severity))
        {
            serializableNamingRule = null;
            return false;
        }

        serializableNamingRule = new SerializableNamingRule()
        {
            EnforcementLevel = severity,
            NamingStyleID = namingStyle.ID,
            SymbolSpecificationID = symbolSpec.ID
        };

        return true;
    }

    private static int GetRulePriority(string namingRuleName, IReadOnlyDictionary<string, string> conventionsDictionary)
        => conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.priority", out var value) &&
           int.TryParse(value, out var result)
            ? result
            : 0;

    internal static bool TryGetRuleSeverity(
        string namingRuleName,
        IReadOnlyDictionary<string, (string value, TextLine? line)> conventionsDictionary,
        out (ReportDiagnostic severity, TextLine? line) value)
        => TryGetRuleSeverity(namingRuleName, conventionsDictionary, x => x.value, x => x.line, out value);

    private static bool TryGetRuleSeverity(
        string namingRuleName,
        IReadOnlyDictionary<string, string> conventionsDictionary,
        out ReportDiagnostic severity)
    {
        var result = TryGetRuleSeverity<string, object?>(
            namingRuleName,
            conventionsDictionary,
            x => x,
            x => null, // we don't have a tuple
            out var tuple);
        severity = tuple.severity;
        return result;
    }

    private static bool TryGetRuleSeverity<T, V>(
        string namingRuleName,
        IReadOnlyDictionary<string, T> conventionsDictionary,
        Func<T, string> valueSelector,
        Func<T, V> partSelector,
        out (ReportDiagnostic severity, V value) value)
    {
        if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.severity", out var result))
        {
            var severity = ParseEnforcementLevel(valueSelector(result) ?? string.Empty);
            value = (severity, partSelector(result));
            return true;
        }

        value = default;
        return false;
    }

    private static ReportDiagnostic ParseEnforcementLevel(string ruleSeverity)
    {
        return ruleSeverity switch
        {
            EditorConfigSeverityStrings.None => ReportDiagnostic.Suppress,
            EditorConfigSeverityStrings.Refactoring or EditorConfigSeverityStrings.Silent => ReportDiagnostic.Hidden,
            EditorConfigSeverityStrings.Suggestion => ReportDiagnostic.Info,
            EditorConfigSeverityStrings.Warning => ReportDiagnostic.Warn,
            EditorConfigSeverityStrings.Error => ReportDiagnostic.Error,
            _ => ReportDiagnostic.Hidden,
        };
    }
}
