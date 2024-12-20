// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal sealed class CodeGenerationOperatorSymbol(
    INamedTypeSymbol containingType,
    ImmutableArray<AttributeData> attributes,
    Accessibility accessibility,
    DeclarationModifiers modifiers,
    ITypeSymbol returnType,
    CodeGenerationOperatorKind operatorKind,
    ImmutableArray<IParameterSymbol> parameters,
    ImmutableArray<AttributeData> returnTypeAttributes,
    string documentationCommentXml) : CodeGenerationMethodSymbol(containingType,
         attributes,
         accessibility,
         modifiers,
         returnType,
         refKind: RefKind.None,
         explicitInterfaceImplementations: default,
         GetMetadataName(operatorKind),
         typeParameters: [],
         parameters,
         returnTypeAttributes,
         documentationCommentXml)
{
    public override MethodKind MethodKind => MethodKind.UserDefinedOperator;

    public static int GetParameterCount(CodeGenerationOperatorKind operatorKind)
    {
        return operatorKind switch
        {
            CodeGenerationOperatorKind.Addition or CodeGenerationOperatorKind.BitwiseAnd or CodeGenerationOperatorKind.BitwiseOr or CodeGenerationOperatorKind.Concatenate or CodeGenerationOperatorKind.Division or CodeGenerationOperatorKind.Equality or CodeGenerationOperatorKind.ExclusiveOr or CodeGenerationOperatorKind.Exponent or CodeGenerationOperatorKind.GreaterThan or CodeGenerationOperatorKind.GreaterThanOrEqual or CodeGenerationOperatorKind.Inequality or CodeGenerationOperatorKind.IntegerDivision or CodeGenerationOperatorKind.LeftShift or CodeGenerationOperatorKind.LessThan or CodeGenerationOperatorKind.LessThanOrEqual or CodeGenerationOperatorKind.Like or CodeGenerationOperatorKind.Modulus or CodeGenerationOperatorKind.Multiplication or CodeGenerationOperatorKind.RightShift or CodeGenerationOperatorKind.UnsignedRightShift or CodeGenerationOperatorKind.Subtraction => 2,
            CodeGenerationOperatorKind.Increment or CodeGenerationOperatorKind.Decrement or CodeGenerationOperatorKind.False or CodeGenerationOperatorKind.LogicalNot or CodeGenerationOperatorKind.OnesComplement or CodeGenerationOperatorKind.True or CodeGenerationOperatorKind.UnaryPlus or CodeGenerationOperatorKind.UnaryNegation => 1,
            _ => throw ExceptionUtilities.UnexpectedValue(operatorKind),
        };
    }

    private static string GetMetadataName(CodeGenerationOperatorKind operatorKind)
        => operatorKind switch
        {
            CodeGenerationOperatorKind.Addition => WellKnownMemberNames.AdditionOperatorName,
            CodeGenerationOperatorKind.BitwiseAnd => WellKnownMemberNames.BitwiseAndOperatorName,
            CodeGenerationOperatorKind.BitwiseOr => WellKnownMemberNames.BitwiseOrOperatorName,
            CodeGenerationOperatorKind.Concatenate => WellKnownMemberNames.ConcatenateOperatorName,
            CodeGenerationOperatorKind.Decrement => WellKnownMemberNames.DecrementOperatorName,
            CodeGenerationOperatorKind.Division => WellKnownMemberNames.DivisionOperatorName,
            CodeGenerationOperatorKind.Equality => WellKnownMemberNames.EqualityOperatorName,
            CodeGenerationOperatorKind.ExclusiveOr => WellKnownMemberNames.ExclusiveOrOperatorName,
            CodeGenerationOperatorKind.Exponent => WellKnownMemberNames.ExponentOperatorName,
            CodeGenerationOperatorKind.False => WellKnownMemberNames.FalseOperatorName,
            CodeGenerationOperatorKind.GreaterThan => WellKnownMemberNames.GreaterThanOperatorName,
            CodeGenerationOperatorKind.GreaterThanOrEqual => WellKnownMemberNames.GreaterThanOrEqualOperatorName,
            CodeGenerationOperatorKind.Increment => WellKnownMemberNames.IncrementOperatorName,
            CodeGenerationOperatorKind.Inequality => WellKnownMemberNames.InequalityOperatorName,
            CodeGenerationOperatorKind.IntegerDivision => WellKnownMemberNames.IntegerDivisionOperatorName,
            CodeGenerationOperatorKind.LeftShift => WellKnownMemberNames.LeftShiftOperatorName,
            CodeGenerationOperatorKind.LessThan => WellKnownMemberNames.LessThanOperatorName,
            CodeGenerationOperatorKind.LessThanOrEqual => WellKnownMemberNames.LessThanOrEqualOperatorName,
            CodeGenerationOperatorKind.Like => WellKnownMemberNames.LikeOperatorName,
            CodeGenerationOperatorKind.LogicalNot => WellKnownMemberNames.LogicalNotOperatorName,
            CodeGenerationOperatorKind.Modulus => WellKnownMemberNames.ModulusOperatorName,
            CodeGenerationOperatorKind.Multiplication => WellKnownMemberNames.MultiplyOperatorName,
            CodeGenerationOperatorKind.OnesComplement => WellKnownMemberNames.OnesComplementOperatorName,
            CodeGenerationOperatorKind.RightShift => WellKnownMemberNames.RightShiftOperatorName,
            CodeGenerationOperatorKind.UnsignedRightShift => WellKnownMemberNames.UnsignedRightShiftOperatorName,
            CodeGenerationOperatorKind.Subtraction => WellKnownMemberNames.SubtractionOperatorName,
            CodeGenerationOperatorKind.True => WellKnownMemberNames.TrueOperatorName,
            CodeGenerationOperatorKind.UnaryPlus => WellKnownMemberNames.UnaryPlusOperatorName,
            CodeGenerationOperatorKind.UnaryNegation => WellKnownMemberNames.UnaryNegationOperatorName,
            _ => throw ExceptionUtilities.UnexpectedValue(operatorKind),
        };
}
