﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private class CharTC : INumericTC<char>
        {
            public static readonly CharTC Instance = new CharTC();

            char INumericTC<char>.MinValue => char.MinValue;

            char INumericTC<char>.MaxValue => char.MaxValue;

            char INumericTC<char>.Zero => (char)0;

            bool INumericTC<char>.Related(BinaryOperatorKind relation, char left, char right)
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

            char INumericTC<char>.Next(char value)
            {
                Debug.Assert(value != char.MaxValue);
                return (char)(value + 1);
            }

            char INumericTC<char>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? (char)0 : constantValue.CharValue;

            string INumericTC<char>.ToString(char c)
            {
                return ObjectDisplay.FormatPrimitive(c, ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.UseQuotes);
            }

            char INumericTC<char>.Prev(char value)
            {
                Debug.Assert(value != char.MinValue);
                return (char)(value - 1);
            }

            char INumericTC<char>.Random(Random random)
            {
                return (char)random.Next((int)char.MinValue, 1 + (int)char.MaxValue);
            }

            ConstantValue INumericTC<char>.ToConstantValue(char value) => ConstantValue.Create(value);
        }
    }
}
