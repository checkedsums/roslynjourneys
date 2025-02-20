﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Roslyn.Utilities;

internal static class IReadOnlyDictionaryExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        return default!;
    }

    public static IEnumerable<T> GetEnumerableMetadata<T>(this IReadOnlyDictionary<string, object> metadata, string name)
    {
        return metadata.GetValueOrDefault(name) switch
        {
            IEnumerable<T> enumerable => enumerable,
            T s => SpecializedCollections.SingletonEnumerable(s),
            _ => [],
        };
    }

    public static IReadOnlyDictionary<TKey, TValue?> AsNullable<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary)
        where TKey : notnull
        where TValue : class
    {
        // this is a safe cast, even though the language doesn't allow the interface to be 'out TValue'
        return dictionary!;
    }
}
