// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// The result of running a script.
    /// </summary>
    public abstract class ScriptState
    {
        /// <summary>
        /// The script that ran to produce this result.
        /// </summary>
        public Script Script { get; }

        /// <summary>
        /// Caught exception originating from the script top-level code.
        /// </summary>
        /// <remarks>
        /// Exceptions are only caught and stored here if the API returning the <see cref="ScriptState"/> is instructed to do so. 
        /// By default they are propagated to the caller of the API.
        /// </remarks>
        public Exception Exception { get; }

        internal ScriptExecutionState ExecutionState { get; }

        internal ScriptState(ScriptExecutionState executionState, Script script, Exception exceptionOpt)
        {
            Debug.Assert(executionState != null);
            Debug.Assert(script != null);

            ExecutionState = executionState;
            Script = script;
            Exception = exceptionOpt;
        }

        internal abstract object GetReturnValue();

        /// <summary>
        /// Continues script execution from the state represented by this instance by running the specified code snippet.
        /// </summary>
        /// <param name="code">The code to be executed.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running <paramref name="code"/>, including all declared variables and return value.</returns>
        public Task<ScriptState<object>> ContinueWithAsync(string code, ScriptOptions options, CancellationToken cancellationToken)
            => ContinueWithAsync<object>(code, options, null, cancellationToken);

        /// <summary>
        /// Continues script execution from the state represented by this instance by running the specified code snippet.
        /// </summary>
        /// <param name="code">The code to be executed.</param>
        /// <param name="options">Options.</param>
        /// <param name="catchException">
        /// If specified, any exception thrown by the script top-level code is passed to <paramref name="catchException"/>.
        /// If it returns true the exception is caught and stored on the resulting <see cref="ScriptState"/>, otherwise the exception is propagated to the caller.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running <paramref name="code"/>, including all declared variables, return value and caught exception (if applicable).</returns>
        public Task<ScriptState<object>> ContinueWithAsync(string code, ScriptOptions options = null, Func<Exception, bool> catchException = null, CancellationToken cancellationToken = default(CancellationToken))
            => Script.ContinueWith<object>(code, options).RunFromAsync(this, catchException, cancellationToken);

        /// <summary>
        /// Continues script execution from the state represented by this instance by running the specified code snippet.
        /// </summary>
        /// <param name="code">The code to be executed.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running <paramref name="code"/>, including all declared variables and return value.</returns>
        public Task<ScriptState<TResult>> ContinueWithAsync<TResult>(string code, ScriptOptions options, CancellationToken cancellationToken)
            => ContinueWithAsync<TResult>(code, options, null, cancellationToken);

        /// <summary>
        /// Continues script execution from the state represented by this instance by running the specified code snippet.
        /// </summary>
        /// <param name="code">The code to be executed.</param>
        /// <param name="options">Options.</param>
        /// <param name="catchException">
        /// If specified, any exception thrown by the script top-level code is passed to <paramref name="catchException"/>.
        /// If it returns true the exception is caught and stored on the resulting <see cref="ScriptState"/>, otherwise the exception is propagated to the caller.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running <paramref name="code"/>, including all declared variables, return value and caught exception (if applicable).</returns>
        public Task<ScriptState<TResult>> ContinueWithAsync<TResult>(string code, ScriptOptions options = null, Func<Exception, bool> catchException = null, CancellationToken cancellationToken = default(CancellationToken))
            => Script.ContinueWith<TResult>(code, options).RunFromAsync(this, catchException, cancellationToken);

        // How do we resolve overloads? We should use the language semantics.
        // https://github.com/dotnet/roslyn/issues/3720
#if TODO
        /// <summary>
        /// Invoke a method declared by the script.
        /// </summary>
        public object Invoke(string name, params object[] args)
        {
            var func = this.FindMethod(name, args != null ? args.Length : 0);
            if (func != null)
            {
                return func(args);
            }

            return null;
        }

        private Func<object[], object> FindMethod(string name, int argCount)
        {
            for (int i = _executionState.Count - 1; i >= 0; i--)
            {
                var sub = _executionState[i];
                if (sub != null)
                {
                    var type = sub.GetType();
                    var method = FindMethod(type, name, argCount);
                    if (method != null)
                    {
                        return (args) => method.Invoke(sub, args);
                    }
                }
            }

            return null;
        }

        private MethodInfo FindMethod(Type type, string name, int argCount)
        {
            return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Create a delegate to a method declared by the script.
        /// </summary>
        public TDelegate CreateDelegate<TDelegate>(string name)
        {
            var delegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);

            for (int i = _executionState.Count - 1; i >= 0; i--)
            {
                var sub = _executionState[i];
                if (sub != null)
                {
                    var type = sub.GetType();
                    var method = FindMatchingMethod(type, name, delegateInvokeMethod);
                    if (method != null)
                    {
                        return (TDelegate)(object)method.CreateDelegate(typeof(TDelegate), sub);
                    }
                }
            }

            return default(TDelegate);
        }

        private MethodInfo FindMatchingMethod(Type instanceType, string name, MethodInfo delegateInvokeMethod)
        {
            var dprms = delegateInvokeMethod.GetParameters();

            foreach (var mi in instanceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (mi.Name == name)
                {
                    var prms = mi.GetParameters();
                    if (prms.Length == dprms.Length)
                    {
                        // TODO: better matching..
                        return mi;
                    }
                }
            }

            return null;
        }
#endif
    }

    public sealed class ScriptState<T> : ScriptState
    {
        public T ReturnValue { get; }
        internal override object GetReturnValue() => ReturnValue;

        internal ScriptState(ScriptExecutionState executionState, Script script, T value, Exception exceptionOpt)
            : base(executionState, script, exceptionOpt)
        {
            ReturnValue = value;
        }
    }
}
