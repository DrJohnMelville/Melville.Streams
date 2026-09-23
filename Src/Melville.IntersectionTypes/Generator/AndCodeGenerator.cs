using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace Melville.IntersectionTypes.Generator;

public record struct AndCodeGenerator(StructDeclarationSyntax Declaration, SemanticModel SemanticModel)
{
    public void Generate(SourceProductionContext context)
    {
        var symbol = SemanticModel.GetDeclaredSymbol(Declaration) as INamedTypeSymbol;
        if (symbol is null) throw new InvalidOperationException("Cannot find target symbol.");
        var components = symbol.GetComponentTypes();

        if (components.Length < 2)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                            ErrorDeclarations.NeedTwoOrMoreTypes,
                            Declaration.GetLocation(),
                            symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                            ));
            return;
        }
        var engine = new InnerAndCodeGenerator(symbol, components);

        string name = engine.TargetFileName();
        string source = engine.ImplementationCode();

        UdpConsole.WriteLine(name);
        UdpConsole.WriteLine(source);

        context.AddSource(name, source);
    }

    public static class UdpConsole
    {
        private static UdpClient? client = null;
        private static UdpClient Client
        {
            get
            {
                client ??= new UdpClient();
                return client;
            }
        }

        public static string WriteLine(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            Client.Send(bytes, bytes.Length, "127.0.0.1", 15321);
            return str;
        }
    }

    private void CheckComponentTypes(
        ImmutableArray<IParameterSymbol> components, SourceProductionContext context, INamedTypeSymbol symbol)
    {
        if (components.Length < 2)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ErrorDeclarations.NeedTwoOrMoreTypes,
                Declaration.GetLocation(),
                symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                ));
        }
            
    }
}

public static class GetComponentTypesImpl
{
    extension (ITypeSymbol symbol)
    {
        public ImmutableArray<IParameterSymbol> GetComponentTypes() =>
            (symbol.GetMembers("IsIntersectionOfTypes") is { Length: 1} members &&
                members[0] is IMethodSymbol member)?
                member.Parameters:[];

    }
}
