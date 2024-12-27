// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct ResultProperties(
        DkmClrCompilationResultFlags flags,
        DkmEvaluationResultCategory category,
        DkmEvaluationResultAccessType accessType,
        DkmEvaluationResultStorageType storageType,
        DkmEvaluationResultTypeModifierFlags modifierFlags)
    {
        public readonly DkmClrCompilationResultFlags Flags = flags;
        public readonly DkmEvaluationResultCategory Category = category;
        public readonly DkmEvaluationResultAccessType AccessType = accessType;
        public readonly DkmEvaluationResultStorageType StorageType = storageType;
        public readonly DkmEvaluationResultTypeModifierFlags ModifierFlags = modifierFlags;

        /// <remarks>
        /// For statements and assignments, we are only interested in <see cref="DkmClrCompilationResultFlags"/>.
        /// </remarks>
        public ResultProperties(DkmClrCompilationResultFlags flags)
            : this(flags, category: default, accessType: default, storageType: default, modifierFlags: default) { }
    }
}
