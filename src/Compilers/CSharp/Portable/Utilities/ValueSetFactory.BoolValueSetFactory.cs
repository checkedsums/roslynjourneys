﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A value set factory for boolean values.
        /// </summary>
        private sealed class BoolValueSetFactory : IValueSetFactory<bool>
        {
            public static readonly BoolValueSetFactory Instance = new BoolValueSetFactory();

            private BoolValueSetFactory() { }

            IValueSet IValueSetFactory.AllValues => BoolValueSet.AllValues;

            IValueSet IValueSetFactory.NoValues => BoolValueSet.None;

            public IValueSet<bool> Related(BinaryOperatorKind relation, bool value)
            {
                return (relation, value)
switch
                {
                    (Equal, true) => BoolValueSet.OnlyTrue,
                    (Equal, false) => BoolValueSet.OnlyFalse,
                    _ => BoolValueSet.AllValues,// for error recovery
                };
            }

            IValueSet IValueSetFactory.Random(int expectedSize, Random random) => random.Next(4) switch
            {
                0 => BoolValueSet.None,
                1 => BoolValueSet.OnlyFalse,
                2 => BoolValueSet.OnlyTrue,
                3 => BoolValueSet.AllValues,
                _ => throw ExceptionUtilities.UnexpectedValue("random"),
            };

            ConstantValue IValueSetFactory.RandomValue(Random random) => ConstantValue.Create(random.NextDouble() < 0.5);

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value)
            {
                return value.IsBad ? BoolValueSet.AllValues : Related(relation, value.BooleanValue);
            }

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                Debug.Assert(relation == BinaryOperatorKind.Equal);
                return left.IsBad || right.IsBad || left.BooleanValue == right.BooleanValue;
            }
        }
    }
}
