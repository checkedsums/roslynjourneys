// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// If the record type is derived from a base record type Base, the record type includes
    /// a synthesized override of the strongly-typed Equals(Base other). The synthesized
    /// override is sealed. It is an error if the override is declared explicitly.
    /// The synthesized override returns Equals((object?)other).
    /// </summary>
    internal sealed class SynthesizedRecordBaseEquals(SourceMemberContainerTypeSymbol containingType, int memberOffset) :
    SynthesizedRecordOrdinaryMethod(containingType, WellKnownMemberNames.ObjectEquals, memberOffset, DeclarationModifiers.Public | DeclarationModifiers.Override | DeclarationModifiers.Sealed)
    {
        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: [new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(ContainingType.BaseTypeNoUseSiteDiagnostics, NullableAnnotation.Annotated),
                                                                    ordinal: 0, RefKind.None, "other", Locations)]);
        }

        protected override int GetParameterCountFromSyntax() => 1;

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            base.MethodChecks(diagnostics);

            var overridden = OverriddenMethod;

            if (overridden is not null &&
                !overridden.ContainingType.Equals(ContainingType.BaseTypeNoUseSiteDiagnostics, TypeCompareKind.AllIgnoreOptions))
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideBaseMethod, GetFirstLocation(), this, ContainingType.BaseTypeNoUseSiteDiagnostics);
            }
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var f = new SyntheticBoundNodeFactory(this, this.SyntaxNode, compilationState, diagnostics);

            try
            {
                ParameterSymbol parameter = Parameters[0];

                if (parameter.Type.IsErrorType())
                {
                    f.CloseMethod(f.ThrowNull());
                    return;
                }

                var retExpr = f.Call(
                    f.This(),
                    ContainingType.GetMembersUnordered().OfType<SynthesizedRecordObjEquals>().Single(),
                    f.Convert(f.SpecialType(SpecialType.System_Object), f.Parameter(parameter)));

                f.CloseMethod(f.Block(f.Return(retExpr)));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                f.CloseMethod(f.ThrowNull());
            }
        }
    }
}
