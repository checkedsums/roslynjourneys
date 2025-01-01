// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class ConversionsBase
    {
        public static void AddTypesParticipatingInUserDefinedConversion(ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol? ConstrainedToTypeOpt)> result, TypeSymbol sourceType, TypeSymbol targetType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            AddTypesParticipatingInUserDefinedConversion(result, sourceType, ref useSiteInfo);
            AddTypesParticipatingInUserDefinedConversion(result, targetType, ref useSiteInfo);
        }

        public static void AddTypesParticipatingInUserDefinedConversion(ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol? ConstrainedToTypeOpt)> result, TypeSymbol type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (type is null)
            {
                return;
            }

            type = type.StrippedType();

            // optimization:
            bool excludeExisting = result.Count > 0;

            if (type is TypeParameterSymbol typeParameter)
            {
                NamedTypeSymbol effectiveBaseClass = typeParameter.EffectiveBaseClass(ref useSiteInfo);
                addFromClassOrStruct(result, excludeExisting, effectiveBaseClass, ref useSiteInfo);

                ImmutableArray<NamedTypeSymbol> interfaces = typeParameter.AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo);

                foreach (NamedTypeSymbol iface in interfaces)
                {
                    if (!excludeExisting || !HasIdentityConversionToAny(iface, result!))
                    {
                        result.Add((iface, typeParameter));
                    }
                }
            }
            else
            {
                addFromClassOrStruct(result, excludeExisting, type, ref useSiteInfo);
            }

            static void addFromClassOrStruct(ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol? ConstrainedToTypeOpt)> result, bool excludeExisting, TypeSymbol type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                if (type.IsClassType() || type.IsStructType())
                {
                    var namedType = (NamedTypeSymbol)type;
                    if (!excludeExisting || !HasIdentityConversionToAny(namedType, result!))
                    {
                        result.Add((namedType, null));
                    }
                }

                NamedTypeSymbol t = type.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
                while (t is not null)
                {
                    if (!excludeExisting || !HasIdentityConversionToAny(t, result!))
                    {
                        result.Add((t, null));
                    }

                    t = t.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
                }
            }
        }
    }
}
