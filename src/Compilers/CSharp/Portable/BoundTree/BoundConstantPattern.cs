// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundConstantPattern : BoundPattern
    {
        public BoundConstantPattern(SyntaxNode syntax, BoundExpression value, ConstantValue constantValue, TypeSymbol inputType, TypeSymbol narrowedType, bool hasErrors = false)
            : base(BoundKind.ConstantPattern, syntax, inputType, narrowedType, hasErrors || value.HasErrors())
        {

            RoslynDebug.Assert(value is object, "Field 'value' cannot be null (make the type nullable in BoundNodes.xml to remove this check)");
            RoslynDebug.Assert(constantValue is object, "Field 'constantValue' cannot be null (make the type nullable in BoundNodes.xml to remove this check)");
            RoslynDebug.Assert(inputType is object, "Field 'inputType' cannot be null (make the type nullable in BoundNodes.xml to remove this check)");
            RoslynDebug.Assert(narrowedType is object, "Field 'narrowedType' cannot be null (make the type nullable in BoundNodes.xml to remove this check)");

            this.Value = value;
            this.ConstantValue = constantValue;
        }

        public BoundExpression Value { get; }
        public ConstantValue ConstantValue { get; }

        [DebuggerStepThrough]
        public override BoundNode? Accept(BoundTreeVisitor visitor) => visitor.VisitConstantPattern(this);

        public BoundConstantPattern Update(BoundExpression value, ConstantValue constantValue, TypeSymbol inputType, TypeSymbol narrowedType)
        {
            if (value != this.Value || constantValue != this.ConstantValue || !TypeSymbol.Equals(inputType, this.InputType, TypeCompareKind.ConsiderEverything) || !TypeSymbol.Equals(narrowedType, this.NarrowedType, TypeCompareKind.ConsiderEverything))
            {
                var result = new BoundConstantPattern(this.Syntax, value, constantValue, inputType, narrowedType, this.HasErrors);
                result.CopyAttributes(this);
                return result;
            }
            return this;
        }
    }
}
