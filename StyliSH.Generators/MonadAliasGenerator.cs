using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyliSH.Generators;

[Generator]
public sealed class MonadAliasGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor Styl001 = new(
        id: "STYL001",
        title: "MonadAlias requires partial",
        messageFormat: "Struct '{0}' annotated with [MonadAlias] must be declared as partial",
        category: "StyliSH.Generators",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor Styl002 = new(
        id: "STYL002",
        title: "MonadAlias requires exactly one type parameter",
        messageFormat: "Struct '{0}' annotated with [MonadAlias] must have exactly one type parameter, but has {1}",
        category: "StyliSH.Generators",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor Styl003 = new(
        id: "STYL003",
        title: "MonadAlias inner marker must implement IMonadMarker",
        messageFormat: "Type '{0}' does not implement IMonadMarker<{0}>",
        category: "StyliSH.Generators",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "StyliSH.Abstractions.Monads.Aliases.MonadAliasAttribute",
                predicate: static (node, _) =>
                    node is RecordDeclarationSyntax rds &&
                    rds.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
                transform: static (ctx, ct) => ExtractResult(ctx, ct))
            .Where(static r => r is not null);

        context.RegisterSourceOutput(
            results.Where(static r => r!.Model is not null).Select(static (r, _) => r!.Model!),
            static (spc, model) => Generate(spc, model));

        context.RegisterSourceOutput(
            results.Where(static r => r!.Diagnostic is not null).Select(static (r, _) => r!.Diagnostic!),
            static (spc, diag) => spc.ReportDiagnostic(diag.ToDiagnostic()));
    }

    private static GenerationResult? ExtractResult(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol structSymbol) return null;

        var syntaxNode = (RecordDeclarationSyntax)ctx.TargetNode;
        var structName = structSymbol.Name;

        // STYL001: must be partial
        bool isPartial = false;
        foreach (var modifier in syntaxNode.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PartialKeyword)) { isPartial = true; break; }
        }
        if (!isPartial)
            return new GenerationResult(null, DiagnosticData.From(Styl001, syntaxNode, structName, ""));

        // STYL002: exactly one type parameter
        if (structSymbol.TypeParameters.Length != 1)
            return new GenerationResult(null, DiagnosticData.From(Styl002, syntaxNode, structName,
                structSymbol.TypeParameters.Length.ToString()));

        // Extract inner marker type from attribute argument
        var attribute = ctx.Attributes[0];
        if (attribute.ConstructorArguments.Length == 0) return null;
        var typeArg = attribute.ConstructorArguments[0];
        if (typeArg.Kind != TypedConstantKind.Type || typeArg.Value is not ITypeSymbol innerMarkerType) return null;

        // STYL003: inner marker must implement IMonadMarker<TSelf>
        var monadMarkerInterface = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("StyliSH.Abstractions.Monads.IMonadMarker`1");
        if (monadMarkerInterface is not null && !ImplementsMonadMarker(innerMarkerType, monadMarkerInterface))
        {
            var innerName = innerMarkerType.ToDisplayString();
            return new GenerationResult(null, DiagnosticData.From(Styl003, syntaxNode, innerName, innerName));
        }

        var namespaceName = structSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var typeParamName = structSymbol.TypeParameters[0].Name;
        var markerName = structName + "Marker";
        var accessibility = structSymbol.DeclaredAccessibility == Accessibility.Public ? "public" : "internal";
        var innerMarkerFullName = innerMarkerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new GenerationResult(
            new MonadAliasModel(namespaceName, structName, typeParamName, innerMarkerFullName, markerName, accessibility),
            null);
    }

    private static bool ImplementsMonadMarker(ITypeSymbol type, INamedTypeSymbol monadMarkerInterface)
    {
        foreach (var iface in type.AllInterfaces)
            if (iface.OriginalDefinition.Equals(monadMarkerInterface, SymbolEqualityComparer.Default))
                return true;
        return false;
    }

    private static void Generate(SourceProductionContext spc, MonadAliasModel model)
    {
        spc.AddSource($"{model.StructName}.MonadAlias.g.cs", GenerateStructSource(model));
        spc.AddSource($"{model.MarkerName}.MonadAlias.g.cs", GenerateMarkerSource(model));
    }

    private static string GenerateStructSource(MonadAliasModel m)
    {
        var markerFullName = $"global::{m.Namespace}.{m.MarkerName}";
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {m.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"{m.Accessibility} readonly partial record struct {m.StructName}<{m.TypeParameterName}>");
        sb.AppendLine($"    : global::StyliSH.Abstractions.Monads.IMonad<{markerFullName}, {m.TypeParameterName}>,");
        sb.AppendLine($"      global::StyliSH.Abstractions.Monads.IMonadUnwrapper<{m.StructName}<{m.TypeParameterName}>, {markerFullName}, {m.TypeParameterName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public required global::StyliSH.Abstractions.Monads.IMonad<{m.InnerMarkerFullName}, {m.TypeParameterName}> Inner {{ get; init; }}");
        sb.AppendLine();
        sb.AppendLine($"    public global::StyliSH.Abstractions.Monads.IMonad<{markerFullName}, TNewValue> RawMap<TNewValue>(");
        sb.AppendLine($"        global::System.Func<{m.TypeParameterName}, TNewValue> map)");
        sb.AppendLine($"        => new {m.StructName}<TNewValue> {{ Inner = Inner.RawMap(map) }};");
        sb.AppendLine();
        sb.AppendLine($"    public global::StyliSH.Abstractions.Monads.IMonad<{markerFullName}, TNewValue> RawBind<TNewValue>(");
        sb.AppendLine($"        global::System.Func<{m.TypeParameterName}, global::StyliSH.Abstractions.Monads.IMonad<{markerFullName}, TNewValue>> bind)");
        sb.AppendLine($"        => new {m.StructName}<TNewValue>");
        sb.AppendLine("        {");
        sb.AppendLine("            Inner = Inner.RawBind(value =>");
        sb.AppendLine($"                (({m.StructName}<TNewValue>)bind(value)).Inner)");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine($"    public static implicit operator {m.StructName}<{m.TypeParameterName}>(");
        sb.AppendLine($"        global::StyliSH.Abstractions.Monads.MonadWrapper<{markerFullName}, {m.TypeParameterName}> monad)");
        sb.AppendLine($"        => global::StyliSH.Abstractions.Monads.IMonadUnwrapper<{m.StructName}<{m.TypeParameterName}>, {markerFullName}, {m.TypeParameterName}>");
        sb.AppendLine("            .CastFrom(monad);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateMarkerSource(MonadAliasModel m)
    {
        var markerFullName = $"global::{m.Namespace}.{m.MarkerName}";
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {m.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"{m.Accessibility} readonly record struct {m.MarkerName}");
        sb.AppendLine($"    : global::StyliSH.Abstractions.Monads.IMonadMarker<{markerFullName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public static global::StyliSH.Abstractions.Monads.IMonad<{markerFullName}, T> Pure<T>(T value)");
        sb.AppendLine($"        => new {m.StructName}<T> {{ Inner = {m.InnerMarkerFullName}.Pure(value) }};");
        sb.AppendLine("}");
        return sb.ToString();
    }
}

internal sealed record MonadAliasModel(
    string Namespace,
    string StructName,
    string TypeParameterName,
    string InnerMarkerFullName,
    string MarkerName,
    string Accessibility);

internal sealed record GenerationResult(MonadAliasModel? Model, DiagnosticData? Diagnostic);

internal sealed record DiagnosticData(
    DiagnosticDescriptor Descriptor,
    string FilePath,
    int StartLine,
    int StartCharacter,
    string Arg0,
    string Arg1)
{
    public static DiagnosticData From(DiagnosticDescriptor descriptor, SyntaxNode node, string arg0, string arg1)
    {
        var span = node.GetLocation().GetLineSpan();
        return new DiagnosticData(
            descriptor,
            span.Path,
            span.StartLinePosition.Line,
            span.StartLinePosition.Character,
            arg0,
            arg1);
    }

    public Diagnostic ToDiagnostic()
    {
        var location = Location.Create(
            FilePath,
            TextSpan.FromBounds(0, 0),
            new LinePositionSpan(
                new LinePosition(StartLine, StartCharacter),
                new LinePosition(StartLine, StartCharacter)));
        return Diagnostic.Create(Descriptor, location, Arg0, Arg1);
    }
}
