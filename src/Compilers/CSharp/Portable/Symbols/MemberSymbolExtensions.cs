// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// SymbolExtensions for member symbols.
    /// </summary>
    internal static partial class SymbolExtensions
    {
        internal static bool HasParamsParameter(this Symbol member)
        {
            var @params = member.GetParameters();
            return !@params.IsEmpty && @params.Last().IsParams;
        }

        /// <summary>
        /// Get the parameters of a member symbol.  Should be a method, property, or event.
        /// </summary>
        internal static ImmutableArray<ParameterSymbol> GetParameters(this Symbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)member).Parameters,
                SymbolKind.Property => ((PropertySymbol)member).Parameters,
                SymbolKind.Event => ImmutableArray<ParameterSymbol>.Empty,
                _ => throw ExceptionUtilities.UnexpectedValue(member.Kind),
            };
        }

        /// <summary>
        /// Get the types of the parameters of a member symbol.  Should be a method, property, or event.
        /// </summary>
        internal static ImmutableArray<TypeWithAnnotations> GetParameterTypes(this Symbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)member).ParameterTypesWithAnnotations,
                SymbolKind.Property => ((PropertySymbol)member).ParameterTypesWithAnnotations,
                SymbolKind.Event => ImmutableArray<TypeWithAnnotations>.Empty,
                _ => throw ExceptionUtilities.UnexpectedValue(member.Kind),
            };
        }

        internal static bool GetIsVararg(this Symbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)member).IsVararg,
                SymbolKind.Property or SymbolKind.Event => false,
                _ => throw ExceptionUtilities.UnexpectedValue(member.Kind),
            };
        }

        /// <summary>
        /// Get the ref kinds of the parameters of a member symbol.  Should be a method, property, or event.
        /// </summary>
        internal static ImmutableArray<RefKind> GetParameterRefKinds(this Symbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)member).ParameterRefKinds,
                SymbolKind.Property => ((PropertySymbol)member).ParameterRefKinds,
                SymbolKind.Event => default,
                _ => throw ExceptionUtilities.UnexpectedValue(member.Kind),
            };
        }

        internal static int GetParameterCount(this Symbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)member).ParameterCount,
                SymbolKind.Property => ((PropertySymbol)member).ParameterCount,
                SymbolKind.Event or SymbolKind.Field => 0,
                _ => throw ExceptionUtilities.UnexpectedValue(member.Kind),
            };
        }

        internal static bool HasParameterContainingPointerType(this Symbol member)
        {
            foreach (var parameterType in member.GetParameterTypes())
            {
                if (parameterType.Type.ContainsPointer())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEventOrPropertyWithImplementableNonPublicAccessor(this Symbol symbol)
        {
            Debug.Assert(symbol.ContainingType.IsInterface);

            switch (symbol.Kind)
            {
                case SymbolKind.Property:
                    var propertySymbol = (PropertySymbol)symbol;
                    return isImplementableAndNotPublic(propertySymbol.GetMethod) || isImplementableAndNotPublic(propertySymbol.SetMethod);

                case SymbolKind.Event:
                    var eventSymbol = (EventSymbol)symbol;
                    return isImplementableAndNotPublic(eventSymbol.AddMethod) || isImplementableAndNotPublic(eventSymbol.RemoveMethod);
            }

            return false;

            bool isImplementableAndNotPublic(MethodSymbol accessor)
            {
                return accessor.IsImplementable() && accessor.DeclaredAccessibility != Accessibility.Public;
            }
        }

        public static bool IsImplementable(this MethodSymbol methodOpt)
        {
            return methodOpt is not null && !methodOpt.IsSealed && (methodOpt.IsAbstract || methodOpt.IsVirtual);
        }

        public static bool IsAccessor(this MethodSymbol methodSymbol)
        {
            return methodSymbol.AssociatedSymbol is not null;
        }

        public static bool IsAccessor(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && IsAccessor((MethodSymbol)symbol);
        }

        public static bool IsIndexedPropertyAccessor(this MethodSymbol methodSymbol)
        {
            var propertyOrEvent = methodSymbol.AssociatedSymbol;
            return (propertyOrEvent is not null) && propertyOrEvent.IsIndexedProperty();
        }

        public static bool IsOperator(this MethodSymbol methodSymbol)
        {
            return methodSymbol.MethodKind == MethodKind.UserDefinedOperator || methodSymbol.MethodKind == MethodKind.Conversion;
        }

        public static bool IsOperator(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && IsOperator((MethodSymbol)symbol);
        }

        public static bool IsIndexer(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Property && ((PropertySymbol)symbol).IsIndexer;
        }

        public static bool IsIndexedProperty(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Property && ((PropertySymbol)symbol).IsIndexedProperty;
        }

        public static bool IsUserDefinedConversion(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && ((MethodSymbol)symbol).MethodKind == MethodKind.Conversion;
        }

        /// <summary>
        /// Count the number of custom modifiers in/on the return type
        /// and parameters of the specified method.
        /// </summary>
        public static int CustomModifierCount(this MethodSymbol method)
        {
            int count = 0;

            var methodReturnType = method.ReturnTypeWithAnnotations;
            count += methodReturnType.CustomModifiers.Length + method.RefCustomModifiers.Length;
            count += methodReturnType.Type.CustomModifierCount();

            foreach (ParameterSymbol param in method.Parameters)
            {
                var paramType = param.TypeWithAnnotations;
                count += paramType.CustomModifiers.Length + param.RefCustomModifiers.Length;
                count += paramType.Type.CustomModifierCount();
            }

            return count;
        }

        public static int CustomModifierCount(this Symbol m)
        {
            return m.Kind switch
            {
                SymbolKind.ArrayType or SymbolKind.ErrorType or SymbolKind.NamedType or SymbolKind.PointerType or SymbolKind.TypeParameter or SymbolKind.FunctionPointerType => ((TypeSymbol)m).CustomModifierCount(),
                SymbolKind.Event => ((EventSymbol)m).CustomModifierCount(),
                SymbolKind.Method => ((MethodSymbol)m).CustomModifierCount(),
                SymbolKind.Property => ((PropertySymbol)m).CustomModifierCount(),
                _ => throw ExceptionUtilities.UnexpectedValue(m.Kind),
            };
        }

        public static int CustomModifierCount(this EventSymbol e)
        {
            return e.Type.CustomModifierCount();
        }

        /// <summary>
        /// Count the number of custom modifiers in/on the type
        /// and parameters (for indexers) of the specified property.
        /// </summary>
        public static int CustomModifierCount(this PropertySymbol property)
        {
            int count = 0;

            var type = property.TypeWithAnnotations;
            count += type.CustomModifiers.Length + property.RefCustomModifiers.Length;
            count += type.Type.CustomModifierCount();

            foreach (ParameterSymbol param in property.Parameters)
            {
                var paramType = param.TypeWithAnnotations;
                count += paramType.CustomModifiers.Length + param.RefCustomModifiers.Length;
                count += paramType.Type.CustomModifierCount();
            }

            return count;
        }

        internal static Symbol SymbolAsMember(this Symbol s, NamedTypeSymbol newOwner)
        {
            return s.Kind switch
            {
                SymbolKind.Field => ((FieldSymbol)s).AsMember(newOwner),
                SymbolKind.Method => ((MethodSymbol)s).AsMember(newOwner),
                SymbolKind.NamedType => ((NamedTypeSymbol)s).AsMember(newOwner),
                SymbolKind.Property => ((PropertySymbol)s).AsMember(newOwner),
                SymbolKind.Event => ((EventSymbol)s).AsMember(newOwner),
                _ => throw ExceptionUtilities.UnexpectedValue(s.Kind),
            };
        }

        /// <summary>
        /// Return the arity of a member.
        /// </summary>
        internal static int GetMemberArity(this Symbol symbol)
        {
            return symbol.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)symbol).Arity,
                SymbolKind.NamedType or SymbolKind.ErrorType => ((NamedTypeSymbol)symbol).Arity,
                _ => 0,
            };
        }

        internal static NamespaceOrTypeSymbol OfMinimalArity(this IEnumerable<NamespaceOrTypeSymbol> symbols)
        {
            NamespaceOrTypeSymbol minAritySymbol = null;
            int minArity = Int32.MaxValue;
            foreach (var symbol in symbols)
            {
                int arity = GetMemberArity(symbol);
                if (arity < minArity)
                {
                    minArity = arity;
                    minAritySymbol = symbol;
                }
            }

            return minAritySymbol;
        }

        internal static ImmutableArray<TypeParameterSymbol> GetMemberTypeParameters(this Symbol symbol)
        {
            return symbol.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)symbol).TypeParameters,
                SymbolKind.NamedType or SymbolKind.ErrorType => ((NamedTypeSymbol)symbol).TypeParameters,
                SymbolKind.Field or SymbolKind.Property or SymbolKind.Event => ImmutableArray<TypeParameterSymbol>.Empty,
                _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind),
            };
        }

        internal static ImmutableArray<TypeSymbol> GetMemberTypeArgumentsNoUseSiteDiagnostics(this Symbol symbol)
        {
            return symbol.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)symbol).TypeArgumentsWithAnnotations.SelectAsArray(TypeMap.AsTypeSymbol),
                SymbolKind.NamedType or SymbolKind.ErrorType => ((NamedTypeSymbol)symbol).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.SelectAsArray(TypeMap.AsTypeSymbol),
                SymbolKind.Field or SymbolKind.Property or SymbolKind.Event => ImmutableArray<TypeSymbol>.Empty,
                _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind),
            };
        }

        internal static bool IsConstructor(this MethodSymbol method)
        {
            return method.MethodKind switch
            {
                MethodKind.Constructor or MethodKind.StaticConstructor => true,
                _ => false,
            };
        }

        /// <summary>
        /// Returns true if the method is a constructor and has a this() constructor initializer.
        /// </summary>
        internal static bool HasThisConstructorInitializer(this MethodSymbol method, out ConstructorInitializerSyntax initializerSyntax)
        {
            if (method is not null && method.MethodKind == MethodKind.Constructor)
            {
                SourceMemberMethodSymbol sourceMethod = method as SourceMemberMethodSymbol;
                if (sourceMethod is not null)
                {
                    ConstructorDeclarationSyntax constructorSyntax = sourceMethod.SyntaxNode as ConstructorDeclarationSyntax;
                    if (constructorSyntax?.Initializer?.Kind() == SyntaxKind.ThisConstructorInitializer)
                    {
                        initializerSyntax = constructorSyntax.Initializer;
                        return true;
                    }
                }
            }

            initializerSyntax = null;
            return false;
        }

        internal static bool IncludeFieldInitializersInBody(this MethodSymbol methodSymbol)
        {
            // A struct constructor that calls ": this()" will need to include field initializers if the
            // parameterless constructor is a synthesized default constructor that is not emitted.

            return methodSymbol.IsConstructor()
                && !(methodSymbol.HasThisConstructorInitializer(out var initializerSyntax) && !methodSymbol.ContainingType.IsDefaultValueTypeConstructor(initializerSyntax))
                && !(methodSymbol is SynthesizedRecordCopyCtor) // A record copy constructor is special, regular initializers are not supposed to be executed by it.
                && !Binder.IsUserDefinedRecordCopyConstructor(methodSymbol);
        }

        internal static bool IsDefaultValueTypeConstructor(this NamedTypeSymbol type, ConstructorInitializerSyntax initializerSyntax)
        {
            if (initializerSyntax.ArgumentList.Arguments.Count > 0 || !type.IsValueType)
            {
                return false;
            }

            // If exactly one parameterless constructor, return whether it's the default value type constructor
            // Otherwise, return false
            bool foundParameterlessCtor = false;
            bool result = false;
            foreach (var constructor in type.InstanceConstructors)
            {
                if (constructor.ParameterCount != 0)
                {
                    continue;
                }

                if (foundParameterlessCtor)
                {
                    // finding more than one parameterless constructor is an error scenario (reported elsewhere)
                    return false;
                }

                foundParameterlessCtor = true;
                result = constructor.IsDefaultValueTypeConstructor();
            }

            return result;
        }

        /// <summary>
        /// NOTE: every struct has a public parameterless constructor either user-defined or default one
        /// </summary>
        internal static bool IsParameterlessConstructor(this MethodSymbol method)
        {
            return method.MethodKind == MethodKind.Constructor && method.ParameterCount == 0;
        }

#nullable enable
        /// <summary>
        /// Returns true if the method is the default constructor synthesized for struct types.
        /// If the containing struct type is from metadata, the default constructor is synthesized when there
        /// is no accessible parameterless constructor. (That synthesized constructor from metadata zero-inits
        /// the instance.) If the containing struct type is from source, the parameterless constructor is synthesized
        /// if there is no explicit parameterless constructor, and the synthesized
        /// parameterless constructor simply zero-inits the instance (and is not emitted).
        /// </summary>
        internal static bool IsDefaultValueTypeConstructor(this MethodSymbol method)
        {
            return method.IsImplicitlyDeclared &&
                method.ContainingType?.IsValueType == true &&
                method.IsParameterlessConstructor();
        }
#nullable disable

        /// <summary>
        /// Indicates whether the method should be emitted.
        /// </summary>
        internal static bool ShouldEmit(this MethodSymbol method)
        {
            // Don't emit the default value type constructor - the runtime handles that
            if (method.IsDefaultValueTypeConstructor())
            {
                return false;
            }

            if (method is SynthesizedStaticConstructor cctor && !cctor.ShouldEmit())
            {
                return false;
            }

            // Don't emit partial methods without an implementation part.
            if (method.IsPartialMember() && method.PartialImplementationPart is null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// If the event has a AddMethod, return that.  Otherwise check the overridden
        /// event, if any.  Repeat for each overridden event.
        /// </summary>
        /// <remarks>
        /// This method exists to mimic the behavior of GetOwnOrInheritedGetMethod, but it
        /// should only ever look at the overridden event in error scenarios.
        /// </remarks>
        internal static MethodSymbol GetOwnOrInheritedAddMethod(this EventSymbol @event)
        {
            while (@event is not null)
            {
                MethodSymbol addMethod = @event.AddMethod;
                if (addMethod is not null)
                {
                    return addMethod;
                }

                @event = @event.IsOverride ? @event.OverriddenEvent : null;
            }

            return null;
        }

        /// <summary>
        /// If the event has a RemoveMethod, return that.  Otherwise check the overridden
        /// event, if any.  Repeat for each overridden event.
        /// </summary>
        /// <remarks>
        /// This method exists to mimic the behavior of GetOwnOrInheritedSetMethod, but it
        /// should only ever look at the overridden event in error scenarios.
        /// </remarks>
        internal static MethodSymbol GetOwnOrInheritedRemoveMethod(this EventSymbol @event)
        {
            while (@event is not null)
            {
                MethodSymbol removeMethod = @event.RemoveMethod;
                if (removeMethod is not null)
                {
                    return removeMethod;
                }

                @event = @event.IsOverride ? @event.OverriddenEvent : null;
            }

            return null;
        }

        internal static bool IsExplicitInterfaceImplementation(this Symbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)member).IsExplicitInterfaceImplementation,
                SymbolKind.Property => ((PropertySymbol)member).IsExplicitInterfaceImplementation,
                SymbolKind.Event => ((EventSymbol)member).IsExplicitInterfaceImplementation,
                _ => false,
            };
        }

        internal static bool IsPartialMember(this Symbol member)
        {
            Debug.Assert(member.IsDefinition);
            return member
                is SourceOrdinaryMethodSymbol { IsPartial: true }
                or SourcePropertySymbol { IsPartial: true }
                or SourcePropertyAccessorSymbol { IsPartial: true };
        }

        internal static bool IsPartialImplementation(this Symbol member)
        {
            Debug.Assert(member.IsDefinition);
            return member
                is SourceOrdinaryMethodSymbol { IsPartialImplementation: true }
                or SourcePropertySymbol { IsPartialImplementation: true }
                or SourcePropertyAccessorSymbol { IsPartialImplementation: true };
        }

        internal static bool IsPartialDefinition(this Symbol member)
        {
            Debug.Assert(member.IsDefinition);
            return member
                is SourceOrdinaryMethodSymbol { IsPartialDefinition: true }
                or SourcePropertySymbol { IsPartialDefinition: true }
                or SourcePropertyAccessorSymbol { IsPartialDefinition: true };
        }

        internal static bool ContainsTupleNames(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    var method = (MethodSymbol)member;
                    return method.ReturnType.ContainsTupleNames() || method.Parameters.Any(static p => p.Type.ContainsTupleNames());
                case SymbolKind.Property:
                    return ((PropertySymbol)member).Type.ContainsTupleNames();
                case SymbolKind.Event:
                    return ((EventSymbol)member).Type.ContainsTupleNames();
                default:
                    // We currently don't need to use this method for fields or locals
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        internal static ImmutableArray<Symbol> GetExplicitInterfaceImplementations(this Symbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)member).ExplicitInterfaceImplementations.Cast<MethodSymbol, Symbol>(),
                SymbolKind.Property => ((PropertySymbol)member).ExplicitInterfaceImplementations.Cast<PropertySymbol, Symbol>(),
                SymbolKind.Event => ((EventSymbol)member).ExplicitInterfaceImplementations.Cast<EventSymbol, Symbol>(),
                _ => ImmutableArray<Symbol>.Empty,
            };
        }

        internal static Symbol GetOverriddenMember(this Symbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)member).OverriddenMethod,
                SymbolKind.Property => ((PropertySymbol)member).OverriddenProperty,
                SymbolKind.Event => ((EventSymbol)member).OverriddenEvent,
                _ => throw ExceptionUtilities.UnexpectedValue(member.Kind),
            };
        }

        internal static Symbol GetLeastOverriddenMember(this Symbol member, NamedTypeSymbol accessingTypeOpt)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    var method = (MethodSymbol)member;
                    return method.GetConstructedLeastOverriddenMethod(accessingTypeOpt, requireSameReturnType: false);

                case SymbolKind.Property:
                    var property = (PropertySymbol)member;
                    return property.GetLeastOverriddenProperty(accessingTypeOpt);

                case SymbolKind.Event:
                    var evnt = (EventSymbol)member;
                    return evnt.GetLeastOverriddenEvent(accessingTypeOpt);

                default:
                    return member;
            }
        }

        internal static bool IsFieldOrFieldLikeEvent(this Symbol member, out FieldSymbol field)
        {
            switch (member.Kind)
            {
                case SymbolKind.Field:
                    field = (FieldSymbol)member;
                    return true;
                case SymbolKind.Event:
                    field = ((EventSymbol)member).AssociatedField;
                    return field is not null;
                default:
                    field = null;
                    return false;
            }
        }

        internal static string GetMemberCallerName(this Symbol member)
        {
            if (member.Kind == SymbolKind.Method)
            {
                member = ((MethodSymbol)member).AssociatedSymbol ?? member;
            }

            return member.IsIndexer() ? member.MetadataName :
                member.IsExplicitInterfaceImplementation() ? ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(member.Name) :
                member.Name;
        }
    }
}
