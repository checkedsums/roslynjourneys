// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundDiscardPattern : BoundPattern
    {
        public BoundDiscardPattern(SyntaxNode syntax, TypeSymbol inputType, TypeSymbol narrowedType, bool hasErrors)
            : base(BoundKind.DiscardPattern, syntax, inputType, narrowedType, hasErrors)
        {

            RoslynDebug.Assert(inputType is object, "Field 'inputType' cannot be null (make the type nullable in BoundNodes.xml to remove this check)");
            RoslynDebug.Assert(narrowedType is object, "Field 'narrowedType' cannot be null (make the type nullable in BoundNodes.xml to remove this check)");
        }

        public BoundDiscardPattern(SyntaxNode syntax, TypeSymbol inputType, TypeSymbol narrowedType)
            : base(BoundKind.DiscardPattern, syntax, inputType, narrowedType)
        {

            RoslynDebug.Assert(inputType is object, "Field 'inputType' cannot be null (make the type nullable in BoundNodes.xml to remove this check)");
            RoslynDebug.Assert(narrowedType is object, "Field 'narrowedType' cannot be null (make the type nullable in BoundNodes.xml to remove this check)");

        }


        [DebuggerStepThrough]
        public override BoundNode? Accept(BoundTreeVisitor visitor) => visitor.VisitDiscardPattern(this);

        public BoundDiscardPattern Update(TypeSymbol inputType, TypeSymbol narrowedType)
        {
            if (!TypeSymbol.Equals(inputType, this.InputType, TypeCompareKind.ConsiderEverything) || !TypeSymbol.Equals(narrowedType, this.NarrowedType, TypeCompareKind.ConsiderEverything))
            {
                var result = new BoundDiscardPattern(this.Syntax, inputType, narrowedType, this.HasErrors);
                result.CopyAttributes(this);
                return result;
            }
            return this;
        }
    }
}

