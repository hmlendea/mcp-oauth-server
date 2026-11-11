using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace McpOAuthServer.ArchitectureTests
{
    [TestFixture]
    public sealed class SourceConventionRulesTests
    {
        [TestCase("\nCall();", 0)]
        [TestCase("Call(\n\n);", 1)]
        [TestCase("Call(\n);", 0)]
        [TestCase("int[] values = [];", 0)]
        [TestCase("int[] values = [\n\n];", 1)]
        [TestCase("if (true)\n{\n\n}", 1)]
        public void GivenDelimiters_WhenCheckingLayout_ThenOnlyStandaloneClosersRejectBlankLines(
            string body,
            int expectedCount)
        {
            string source = "namespace McpOAuthServer { public static class Registration { " +
                "public static void Run()\n{\n" + body + "\n}\n}\n}";
            string[] violations = [.. CheckSource(source, "Registration.cs")
                .Where(reason => reason.Contains("No blank line", StringComparison.Ordinal))];

            Assert.That(violations, Has.Length.EqualTo(expectedCount));
        }

        [TestCase("DateTime.Parse(value, provider)", 0)]
        [TestCase("DateTimeOffset.Parse(value, provider)", 0)]
        [TestCase("DateTime.Parse(value)", 1)]
        public void GivenDateParsing_WhenCheckingOverloads_ThenFormatProvidersAreRequired(
            string expression,
            int expectedCount)
        {
            string source = "using System; namespace McpOAuthServer { public static class Registration { " +
                "public static object Parse(string value, IFormatProvider provider) => " + expression + "; } }";
            string[] violations = [.. CheckSource(source, "Registration.cs")
                .Where(reason => reason.Contains("Date/time", StringComparison.Ordinal))];

            Assert.That(violations, Has.Length.EqualTo(expectedCount));
        }

        [TestCase("public static IServiceCollection Register(IServiceCollection services) => services;", 0)]
        [TestCase("public static List<string> GetValues(List<string> values) => values;", 2)]
        [TestCase("public static IEnumerable<string> GetValues(IEnumerable<string> values) => values;", 0)]
        public void GivenCollectionContracts_WhenCheckingMethods_ThenOnlyFrameworkContractsAreExcepted(
            string method,
            int expectedCount)
        {
            string source = "using System.Collections.Generic; using Microsoft.Extensions.DependencyInjection; " +
                "namespace McpOAuthServer { public sealed class Registration { " + method + " } }";
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            CSharpCompilation compilation = ArchitectureTestSources.Compilations["McpOAuthServer"]
                .RemoveAllSyntaxTrees().AddSyntaxTrees(tree);

            Assert.That(compilation.GetDiagnostics().Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id != "CS0103"), Is.Empty);

            string[] violations = [.. SpecialisedConventionRules.Check(tree, compilation.GetSemanticModel(tree))
                .Where(reason => reason.Contains("collection parameters", StringComparison.Ordinal))];

            Assert.That(violations, Has.Length.EqualTo(expectedCount));
        }

        [TestCase("var value = 42;", 1)]
        [TestCase("int value = 42;", 0)]
        public void GivenALocalDeclaration_WhenCheckingExplicitTypes_ThenVarIsRejected(
            string declaration,
            int expectedCount)
        {
            string source = "namespace McpOAuthServer { public static class Registration { " +
                "public static void Run()\n{\n" + declaration + "\n}\n} }";
            string[] violations = [.. CheckSource(source, "Registration.cs")
                .Where(reason => reason.Contains("explicit local type", StringComparison.Ordinal))];

            Assert.That(violations, Has.Length.EqualTo(expectedCount));
        }

        private static IEnumerable<string> CheckSource(string source, string relativePath)
        {
            string path = Path.Combine(ArchitectureTestSources.Root, "McpOAuthServer", relativePath);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: path);
            CSharpCompilation compilation = ArchitectureTestSources.Compilations["McpOAuthServer"]
                .RemoveAllSyntaxTrees().AddSyntaxTrees(tree);

            Assert.That(compilation.GetDiagnostics().Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id != "CS0103"), Is.Empty);

            return [.. SourceConventionRules.Check(tree, compilation.GetSemanticModel(tree), "McpOAuthServer")];
        }
    }
}