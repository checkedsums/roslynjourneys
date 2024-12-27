// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class AccessibilityExtensions
    {
        public static bool HasProtected(this Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Protected or Accessibility.ProtectedOrInternal or Accessibility.ProtectedAndInternal => true,
                _ => false,
            };
        }
    }
}

