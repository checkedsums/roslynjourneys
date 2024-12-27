// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class EnumConversions
    {
        internal static TypeKind ToTypeKind(this DeclarationKind kind)
        {
            return kind switch
            {
                DeclarationKind.Class or DeclarationKind.Script or DeclarationKind.ImplicitClass or DeclarationKind.Record => TypeKind.Class,
                DeclarationKind.Submission => TypeKind.Submission,
                DeclarationKind.Delegate => TypeKind.Delegate,
                DeclarationKind.Enum => TypeKind.Enum,
                DeclarationKind.Interface => TypeKind.Interface,
                DeclarationKind.Struct or DeclarationKind.RecordStruct => TypeKind.Struct,
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }
    }
}
