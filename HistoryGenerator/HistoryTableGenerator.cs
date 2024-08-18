using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace HistoryGenerator;

[Generator]
public class HistoryTableGenerator : ISourceGenerator
{

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will be created for each generation pass
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Retrieve the populated receiver
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return;

        var classNames = new List<string>();
        // Process each class with the HistoryTable attribute
        foreach (var classDeclaration in receiver.CandidateClasses)
        {
            var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            if (model.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                continue;

            var historyTableAttribute = classSymbol.GetAttributes()
                .FirstOrDefault(ad => ad.AttributeClass?.ToString() == "HistoryGenerator.HistoryTableAttribute");

            if (historyTableAttribute != null)
            {
                var tableName = historyTableAttribute.ConstructorArguments[0].Value?.ToString();
                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var originalClassName = classSymbol.Name;
                var historyClassName = originalClassName + "History";

                // Generate the new class
                var source = GenerateHistoryClass(namespaceName, originalClassName, tableName!);
                context.AddSource($"Data/Models/{historyClassName}.g.cs", source);

                classNames.Add(historyClassName);
            }
        }

        var dbSource = GenerateDbContext(classNames);
        context.AddSource($"Data/AppDbContext.g.cs", dbSource);
    }

    private string GenerateHistoryClass(string namespaceName, string className, string tableName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using System.ComponentModel.DataAnnotations;");
        sb.AppendLine($"using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine("");
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine($"public partial class {className}History");
        sb.AppendLine("{");
        sb.AppendLine("    [Key]");
        sb.AppendLine("    public int Id { get; set; }");
        sb.AppendLine("    [Column(TypeName = \"jsonb\")]");
        sb.AppendLine("    public string Data { get; set; }");
        sb.AppendLine("    public DateTime UpdatedAt { get; set; }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateDbContext(List<string> classNames)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using HistoryGeneratorPOC.Data.Models;");
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("");
        sb.AppendLine($"namespace HistoryGeneratorPOC.Data;");
        sb.AppendLine($"public partial class AppDbContext : DbContext");
        sb.AppendLine("{");
        foreach (var className in classNames)
        {
            sb.AppendLine($"    public DbSet<{className}> {className}s {{ get; set; }}");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }


    private class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Identify classes with the HistoryTable attribute

            if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
                classDeclaration.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "HistoryTable"))
            {
                CandidateClasses.Add(classDeclaration);
            }
        }
    }
}
