﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
#if DEBUG
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
#endif
    partial class BoundDagTest
    {
        public override bool Equals([NotNullWhen(true)] object? obj) => this.Equals(obj as BoundDagTest);

        private bool Equals(BoundDagTest? other)
        {
            if (other is null || this.Kind != other.Kind)
                return false;
            if (this == other)
                return true;
            if (!this.Input.Equals(other.Input))
                return false;

            return (this, other)
switch
            {
                (BoundDagTypeTest x, BoundDagTypeTest y) => x.Type.Equals(y.Type, TypeCompareKind.AllIgnoreOptions),
                (BoundDagNonNullTest x, BoundDagNonNullTest y) => x.IsExplicitTest == y.IsExplicitTest,
                (BoundDagExplicitNullTest x, BoundDagExplicitNullTest y) => true,
                (BoundDagValueTest x, BoundDagValueTest y) => x.Value.Equals(y.Value),
                (BoundDagRelationalTest x, BoundDagRelationalTest y) => x.Relation == y.Relation && x.Value.Equals(y.Value),
                _ => throw ExceptionUtilities.UnexpectedValue(this),
            };
        }

        public override int GetHashCode()
        {
            return Hash.Combine(((int)Kind).GetHashCode(), Input.GetHashCode());
        }

#if DEBUG
        internal new string GetDebuggerDisplay()
        {
            switch (this)
            {
                case BoundDagTypeEvaluation a:
                    return $"{a.GetOutputTempDebuggerDisplay()} = ({a.Type}){a.Input.GetDebuggerDisplay()}";
                case BoundDagPropertyEvaluation e:
                    return $"{e.GetOutputTempDebuggerDisplay()} = {e.Input.GetDebuggerDisplay()}.{e.Property.Name}";
                case BoundDagFieldEvaluation e:
                    return $"{e.GetOutputTempDebuggerDisplay()} = {e.Input.GetDebuggerDisplay()}.{e.Field.Name}";
                case BoundDagDeconstructEvaluation d:
                    var result = "(";
                    var first = true;
                    foreach (var param in d.DeconstructMethod.Parameters)
                    {
                        if (!first)
                        {
                            result += ", ";
                        }
                        first = false;
                        result += $"Item{param.Ordinal + 1}";
                    }
                    result += $") {d.GetOutputTempDebuggerDisplay()} = {d.Input.GetDebuggerDisplay()}";
                    return result;
                case BoundDagIndexEvaluation i:
                    return $"{i.GetOutputTempDebuggerDisplay()} = {i.Input.GetDebuggerDisplay()}[{i.Index}]";
                case BoundDagIndexerEvaluation i:
                    return $"{i.GetOutputTempDebuggerDisplay()} = {i.Input.GetDebuggerDisplay()}[{i.Index}]";
                case BoundDagAssignmentEvaluation i:
                    return $"{i.Target.GetDebuggerDisplay()} <-- {i.Input.GetDebuggerDisplay()}";
                case BoundDagEvaluation e:
                    return $"{e.GetOutputTempDebuggerDisplay()} = {e.Kind}({e.Input.GetDebuggerDisplay()})";
                case BoundDagTypeTest b:
                    var typeName = b.Type.TypeKind == TypeKind.Error ? "<error type>" : b.Type.ToString();
                    return $"{b.Input.GetDebuggerDisplay()} is {typeName}";
                case BoundDagValueTest v:
                    return $"{v.Input.GetDebuggerDisplay()} == {v.Value.GetValueToDisplay()}";
                case BoundDagNonNullTest nn:
                    return $"{nn.Input.GetDebuggerDisplay()} != null";
                case BoundDagExplicitNullTest n:
                    return $"{n.Input.GetDebuggerDisplay()} == null";
                case BoundDagRelationalTest r:
                    var operatorName = r.Relation.Operator() switch
                    {
                        BinaryOperatorKind.LessThan => "<",
                        BinaryOperatorKind.LessThanOrEqual => "<=",
                        BinaryOperatorKind.GreaterThan => ">",
                        BinaryOperatorKind.GreaterThanOrEqual => ">=",
                        _ => "??"
                    };
                    return $"{r.Input.GetDebuggerDisplay()} {operatorName} {r.Value.GetValueToDisplay()}";
                default:
                    return $"{this.Kind}({this.Input.GetDebuggerDisplay()})";
            }
        }
#endif
    }
}
