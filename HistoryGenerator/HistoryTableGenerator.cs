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

        var baseSource = GenerateBaseHistoryClass();
        context.AddSource($"Data/Models/BaseHistory.g.cs", baseSource);

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

                classNames.Add(originalClassName);
            }
        }

        // Figure out the namespace for the DbContext

        var dbSource = GenerateDbContext(classNames, "HistoryGeneratorPOC.Data");
        context.AddSource($"Data/AppDbContext.g.cs", dbSource);
        var interceptorSource = GenerateAuditInterceptors(classNames, "HistoryGeneratorPOC.Data.Models");
        context.AddSource($"Data/AuditInterceptor.g.cs", interceptorSource);
    }

    private string GenerateBaseHistoryClass()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using System.ComponentModel.DataAnnotations;");
        sb.AppendLine($"using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine("");
        sb.AppendLine($"namespace Generated.Data.Models;");
        sb.AppendLine($"public abstract class BaseHistory");
        sb.AppendLine("{");
        sb.AppendLine("    [Key]");
        sb.AppendLine("    public int Id { get; set; }");
        sb.AppendLine("    [Column(TypeName = \"jsonb\")]");
        sb.AppendLine("    public string Data { get; set; }");
        sb.AppendLine("    public DateTime UpdatedAt { get; set; }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateHistoryClass(string namespaceName, string className, string tableName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using System.ComponentModel.DataAnnotations;");
        sb.AppendLine($"using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine("");
        sb.AppendLine($"namespace Generated.Data.Models;");
        sb.AppendLine($"public partial class {className}History: BaseHistory");
        sb.AppendLine("{");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateDbContext(List<string> classNames, string nameSpace)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using Generated.Data.Models;");
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("");
        sb.AppendLine($"namespace {nameSpace};");
        sb.AppendLine($"public partial class AppDbContext : DbContext");
        sb.AppendLine("{");
        foreach (var className in classNames)
        {
            sb.AppendLine($"    public DbSet<{className}History> {className}Historys {{ get; set; }}");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateAuditInterceptors(List<string> classNames, string modelNameSpace)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using Generated.Data.Models;");
        sb.AppendLine($"using {modelNameSpace};");
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine($"using Microsoft.EntityFrameworkCore.Diagnostics;");
        sb.AppendLine($"using System.Text.Json;");
        sb.AppendLine("");
        sb.AppendLine($"namespace Generated.Data;");
        sb.AppendLine($"public partial class AuditInterceptor : SaveChangesInterceptor");
        sb.AppendLine("{");
        sb.AppendLine("    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (eventData.Context is null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return base.SavingChangesAsync(eventData, result, cancellationToken);");
        sb.AppendLine("        }");
        foreach (var className in classNames)
        {
            sb.AppendLine($"        TrackHistory<{className}, {className}History>(eventData.Context);");
        }
        sb.AppendLine("        return base.SavingChangesAsync(eventData, result, cancellationToken);");
        sb.AppendLine("    }");
        sb.AppendLine("    private void TrackHistory<TEntity, THistory>(DbContext context)");
        sb.AppendLine("        where TEntity : class");
        sb.AppendLine("        where THistory : BaseHistory, new()");
        sb.AppendLine("    {");
        sb.AppendLine("        var historyEntries = context.ChangeTracker.Entries()");
        sb.AppendLine("            .Where(x => x.Entity is TEntity");
        sb.AppendLine("            && (x.State is EntityState.Added");
        sb.AppendLine("            || x.State is EntityState.Modified");
        sb.AppendLine("            || x.State is EntityState.Deleted))");
        sb.AppendLine("            .Select(x => new THistory");
        sb.AppendLine("            {");
        sb.AppendLine("                 Id = new Random().Next(),");
        sb.AppendLine("                 Data = JsonSerializer.Serialize(x.Entity),");
        sb.AppendLine("                 UpdatedAt = x.CurrentValues.GetValue<DateTime>(\"UpdatedAt\")");
        sb.AppendLine("            });");
        sb.AppendLine("        if (historyEntries.Any())");
        sb.AppendLine("        {");
        sb.AppendLine("            context.Set<THistory>().AddRange(historyEntries.ToList());");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
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
