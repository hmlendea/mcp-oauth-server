using System;
using System.Linq;

using NUnit.Framework;

namespace McpOAuthServer.ArchitectureTests
{
    [TestFixture]
    public sealed class BoundaryPlacementTests
    {
        [TestCase("Api")]
        [TestCase("Service")]
        [TestCase("DataAccess")]
        [TestCase("Configuration")]
        [TestCase("Logging")]
        public void GivenProductionRoles_WhenCheckingPlacement_ThenBoundariesRemainExplicit(string role)
        {
            string[] violations = [.. BoundaryPlacementRules.Check(
                ArchitectureTestSources.Compilations["McpOAuthServer"],
                role)];

            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        }

        [Test]
        public void GivenTheProductionProject_WhenCheckingRoles_ThenEachRoleSelectsSources()
        {
            string[] expectedRoles = ["Api", "Cli", "Configuration", "DataAccess", "Logging", "Service"];
            string[] actualRoles = [.. ArchitectureTestSources.EnumerateFiles(
                    System.IO.Path.Combine(ArchitectureTestSources.Root, "McpOAuthServer"))
                .Select(ArchitectureTestSources.GetRole)
                .Where(role => !string.Equals(role, "Composition", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(role => role, StringComparer.Ordinal)];

            Assert.That(actualRoles, Is.EquivalentTo(expectedRoles));
        }
    }
}