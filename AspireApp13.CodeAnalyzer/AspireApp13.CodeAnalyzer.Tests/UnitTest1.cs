using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Xunit;

namespace AspireApp13.CodeAnalyzer.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void DuplicateResourceAnalyzer_Should_Report_Duplicate_Resource_Names()
        {
            var testCode = @"
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var ps = ""Resource1"";
                        var ps = ""Resource2"";
                    }
                }";

            var expectedDiagnostic = new DiagnosticResult
            {
                Id = "ASPIRE001",
                Message = "Resources must be unique",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 29)
                }
            };

            VerifyCSharpDiagnostic(testCode, expectedDiagnostic);
        }

        private void VerifyCSharpDiagnostic(string source, params DiagnosticResult[] expected)
        {
            var analyzer = new DuplicateResourceAnalyzer();
            var diagnostics = GetSortedDiagnostics(source, analyzer);
            LogDiagnostics(diagnostics); // Add logging for diagnostics
            VerifyDiagnosticResults(diagnostics, analyzer, expected);
        }

        private Diagnostic[] GetSortedDiagnostics(string source, DiagnosticAnalyzer analyzer)
        {
            var document = CreateDocument(source);
            var compilation = document.Project.GetCompilationAsync().Result;
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        private Document CreateDocument(string source)
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument(documentId, "Test0.cs", SourceText.From(source));
            return solution.GetDocument(documentId);
        }

        private void VerifyDiagnosticResults(Diagnostic[] actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
        {
            var expectedCount = expectedResults.Length;
            var actualCount = actualResults.Length;

            Assert.Equal(expectedCount, actualCount);

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults[i];
                var expected = expectedResults[i];

                Assert.Equal(expected.Id, actual.Id);
                Assert.Equal(expected.Severity, actual.Severity);
                Assert.Equal(expected.Message, actual.GetMessage());

                if (expected.Locations.Length == 0)
                {
                    Assert.True(actual.Location == Location.None);
                }
                else
                {
                    VerifyDiagnosticLocation(actual, actual.Location, expected.Locations.First());
                }
            }
        }

        private void VerifyDiagnosticLocation(Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            Assert.Equal(expected.Path, actualSpan.Path);
            Assert.Equal(expected.Line, actualSpan.StartLinePosition.Line + 1);
            Assert.Equal(expected.Column, actualSpan.StartLinePosition.Character + 1);
        }

        private void LogDiagnostics(Diagnostic[] diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"Diagnostic: {diagnostic.Id}, Message: {diagnostic.GetMessage()}, Location: {diagnostic.Location.GetLineSpan()}");
            }
        }
    }

    public class DiagnosticResult
    {
        public string? Id { get; set; }
        public string? Message { get; set; }
        public DiagnosticSeverity Severity { get; set; }
        public DiagnosticResultLocation[] Locations { get; set; }
    }

    public class DiagnosticResultLocation
    {
        public DiagnosticResultLocation(string path, int line, int column)
        {
            Path = path;
            Line = line;
            Column = column;
        }

        public string Path { get; }
        public int Line { get; }
        public int Column { get; }
    }
}
