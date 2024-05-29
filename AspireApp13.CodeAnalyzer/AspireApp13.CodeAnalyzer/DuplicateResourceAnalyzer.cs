using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AspireApp13.CodeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DuplicateResourceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ASPIRE001";
        private static readonly LocalizableString Title = "Duplicate Resource Name";
        private static readonly LocalizableString MessageFormat = "Resources must be unique";
        private static readonly LocalizableString Description = "Resource names must be unique within the same scope.";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var reportedNames = new HashSet<string>();

                compilationContext.RegisterSyntaxNodeAction(nodeContext =>
                {
                    AnalyzeNode(nodeContext, reportedNames);
                }, SyntaxKind.LocalDeclarationStatement);
            });
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context, HashSet<string> reportedNames)
        {
            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;
            var parentBlock = localDeclaration.Parent as BlockSyntax;

            if (parentBlock == null)
            {
                return;
            }

            var variableNames = new HashSet<string>();

            foreach (var statement in parentBlock.Statements)
            {
                if (statement is LocalDeclarationStatementSyntax declaration)
                {
                    foreach (var variable in declaration.Declaration.Variables)
                    {
                        var variableName = variable.Identifier.Text;
                        if (!variableNames.Add(variableName))
                        {
                            if (!reportedNames.Contains(variableName))
                            {
                                var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), variableName);
                                context.ReportDiagnostic(diagnostic);
                                reportedNames.Add(variableName);
                            }
                        }
                    }
                }
            }
        }
    }
}
