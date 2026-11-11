using System.Collections.Generic;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using McpOAuthServer.Service;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace McpOAuthServer.ArchitectureTests
{
    [TestFixture]
    public sealed class LayerDependencyTests
    {
        private static readonly Architecture architecture = new ArchLoader()
            .LoadAssemblies(typeof(Program).Assembly)
            .Build();

        [TestCase("Api", "McpOAuthServer\\.(Cli|DataAccess)(\\..*)?")]
        [TestCase("Service", "McpOAuthServer\\.(Api|Cli)(\\..*)?")]
        [TestCase("Configuration", "McpOAuthServer\\.(Api|Cli|Service|DataAccess)(\\..*)?")]
        [TestCase("DataAccess", "McpOAuthServer\\.(Api|Cli|Configuration|Service)(\\..*)?")]
        [TestCase("Logging", "McpOAuthServer\\.(Api|Cli|Configuration|DataAccess|Service)(\\..*)?")]
        public void GivenAnArchitecturalArea_WhenCheckingDependencies_ThenForbiddenAreasAreNotReferenced(
            string area,
            string forbiddenTypes)
        {
            IObjectProvider<IType> sourceTypes = Types().That()
                .ResideInNamespaceOrBelow($"McpOAuthServer.{area}");

            Assert.That(
                sourceTypes.GetObjects(architecture),
                Is.Not.Empty,
                $"The {area} boundary must select production types.");

            Types().That().Are(sourceTypes).Should().NotDependOnAny(
                Types().That().HaveFullNameMatching($"^{forbiddenTypes}$"))
                .Because($"The documented {area} boundary prohibits dependencies on {forbiddenTypes}.")
                .Check(architecture);
        }

        [TestCase("Api", "^(NuciDAL|Microsoft\\.EntityFrameworkCore|Amazon\\.DynamoDBv2)(\\..*)?$")]
        [TestCase("Service", "^(NuciDAL\\.Repositories\\.JsonRepository.*|NuciDAL\\.DependencyInjection\\..*|NuciLog\\.NuciLogger|Microsoft\\.AspNetCore\\..*|Microsoft\\.EntityFrameworkCore\\..*|Amazon\\.DynamoDBv2\\..*)$")]
        [TestCase("Service.Models", "^(Microsoft\\.AspNetCore\\..*|Microsoft\\.EntityFrameworkCore\\..*|Amazon\\.DynamoDBv2\\..*|Dapper\\..*|MediatR\\..*)$")]
        public void GivenAnArchitecturalArea_WhenCheckingInfrastructure_ThenOnlyPermittedAbstractionsAreUsed(
            string area,
            string forbiddenTypes)
        {
            IObjectProvider<IType> sourceTypes = Types().That()
                .ResideInNamespaceOrBelow($"McpOAuthServer.{area}");

            Assert.That(
                sourceTypes.GetObjects(architecture),
                Is.Not.Empty,
                $"The {area} infrastructure rule must select production types.");

            Types().That().Are(sourceTypes).Should().NotDependOnAny(
                Types(includeReferenced: true).That().HaveFullNameMatching(forbiddenTypes))
                .Because($"The {area} boundary must not use concrete or unrelated infrastructure.")
                .Check(architecture);
        }

        [TestCase("McpOAuthServer.Api.Controllers.AuthorisationController", "McpOAuthServer.Service.Authorisation.IAuthorisationService")]
        [TestCase("McpOAuthServer.Api.Controllers.TokenController", "McpOAuthServer.Service.Tokens.ITokenExchangeService")]
        [TestCase("McpOAuthServer.Service.Tokens.TokenExchangeService", "McpOAuthServer.Service.Clients.IClientService")]
        [TestCase("McpOAuthServer.Service.Authorisation.AuthorisationService", "McpOAuthServer.Service.AuthorisationCodes.IAuthorisationCodeService")]
        [TestCase("McpOAuthServer.Service.Tokens.TokenService", "NuciLog.Core.ILogger")]
        public void GivenAnApplicationBoundary_WhenCheckingCollaborators_ThenItsAbstractionIsUsed(
            string area,
            string collaborator)
        {
            IObjectProvider<Class> sourceTypes = Classes().That().HaveFullName(area);
            string collaboratorPattern = $"^{collaborator.Replace(".", "\\.")}$";

            Assert.That(sourceTypes.GetObjects(architecture), Is.Not.Empty);
            Assert.That(
                Types(includeReferenced: true).That()
                    .HaveFullNameMatching(collaboratorPattern).GetObjects(architecture),
                Is.Not.Empty,
                $"The required collaborator {collaborator} must be present in the loaded architecture.");

            Types().That().Are(sourceTypes).Should().DependOnAny(
                Types(includeReferenced: true).That().HaveFullNameMatching(collaboratorPattern))
                .Because($"The {area} boundary delegates through {collaborator}.")
                .Check(architecture);
        }

        [Test]
        public void GivenTheServiceLayer_WhenCheckingPersistence_ThenRepositoriesAreOnlyReachedThroughInterfaces()
            => Types().That().ResideInNamespaceOrBelow("McpOAuthServer.Service")
                .Should().NotDependOnAny(
                    Types(includeReferenced: true).That().HaveFullNameMatching("^NuciDAL\\.Repositories\\.JsonRepository.*$"))
                .Because("The application service must depend on IFileRepository, not the concrete JSON repository.")
                .Check(architecture);
    }
}