using Microsoft.CodeAnalysis;

namespace Melville.IntersectionTypes;

public static class ErrorDeclarations
{
    public static readonly DiagnosticDescriptor ParameterLacksType = new(
    id: "And0001",
    title: "Invalid intersecion type arguments",
    messageFormat: "Type {0} does not implement {1}",
    category: "Types",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor NeedTwoOrMoreTypes = new(
        id: "And0002",
        title: "Insufficient type arguments",
        messageFormat:"Type {0} has insufficient component types",
        category: "Types",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
     );

}