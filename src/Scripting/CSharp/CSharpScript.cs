// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters

using System;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Scripting
{
    /// <summary>
    /// A factory for creating and running C# scripts.
    /// </summary>
    public static class CSharpScript
    {
        /// <summary>
        /// Create a new C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globalsType">Type of global object.</param>
        /// <param name="assemblyLoader">Custom  assembly loader.</param>
        /// <typeparam name="T">The return type of the script</typeparam>
        /// <exception cref="ArgumentNullException">Code is null.</exception>
        public static Script<T> Create<T>(string code, ScriptOptions options = null, Type globalsType = null, InteractiveAssemblyLoader assemblyLoader = null)
        {
            return code == null
                ? throw new ArgumentNullException(nameof(code))
                : Script.CreateInitialScript<T>(CSharpScriptCompiler.Instance, SourceText.From(code, options?.FileEncoding, SourceHashAlgorithms.Default), options, globalsType, assemblyLoader);
        }
    }
}

