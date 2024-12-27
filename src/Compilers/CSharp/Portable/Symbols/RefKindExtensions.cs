// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class RefKindExtensions
    {
        public static bool IsManagedReference(this RefKind refKind)
        {
            Debug.Assert(refKind <= RefKind.RefReadOnly);

            return refKind != RefKind.None;
        }

        public static RefKind GetRefKind(this SyntaxKind syntaxKind)
        {
            return syntaxKind switch
            {
                SyntaxKind.RefKeyword => RefKind.Ref,
                SyntaxKind.OutKeyword => RefKind.Out,
                SyntaxKind.InKeyword => RefKind.In,
                SyntaxKind.None => RefKind.None,
                _ => throw ExceptionUtilities.UnexpectedValue(syntaxKind),
            };
        }

        public static bool IsWritableReference(this RefKind refKind)
        {
            return refKind switch
            {
                RefKind.Ref or RefKind.Out => true,
                RefKind.None or RefKind.In or RefKind.RefReadOnlyParameter => false,
                _ => throw ExceptionUtilities.UnexpectedValue(refKind),
            };
        }
    }
}
