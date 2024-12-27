// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private class SByteTC : INumericTC<sbyte>
        {
            public static readonly SByteTC Instance = new SByteTC();
            sbyte INumericTC<sbyte>.MinValue => sbyte.MinValue;

            sbyte INumericTC<sbyte>.MaxValue => sbyte.MaxValue;

            sbyte INumericTC<sbyte>.Zero => 0;

            bool INumericTC<sbyte>.Related(BinaryOperatorKind relation, sbyte left, sbyte right)
            {
                return relation switch
                {
                    Equal => left == right,
                    GreaterThanOrEqual => left >= right,
                    GreaterThan => left > right,
                    LessThanOrEqual => left <= right,
                    LessThan => left < right,
                    _ => throw new ArgumentException("relation"),
                };
            }

            sbyte INumericTC<sbyte>.Next(sbyte value)
            {
                Debug.Assert(value != sbyte.MaxValue);
                return (sbyte)(value + 1);
            }

            sbyte INumericTC<sbyte>.Prev(sbyte value)
            {
                Debug.Assert(value != sbyte.MinValue);
                return (sbyte)(value - 1);
            }

            sbyte INumericTC<sbyte>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? (sbyte)0 : constantValue.SByteValue;

            public ConstantValue ToConstantValue(sbyte value) => ConstantValue.Create(value);

            string INumericTC<sbyte>.ToString(sbyte value) => value.ToString();

            sbyte INumericTC<sbyte>.Random(Random random)
            {
                return (sbyte)random.Next();
            }
        }
    }
}
