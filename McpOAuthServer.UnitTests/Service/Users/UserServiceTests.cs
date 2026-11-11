using System.Collections.Generic;
using System.Linq;

using Moq;

using NuciDAL.DataObjects;
using NuciDAL.Repositories;

using NuciLog.Core;

using NUnit.Framework;

using McpOAuthServer.Configuration;
using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Security;
using McpOAuthServer.Service.Users;

namespace McpOAuthServer.UnitTests.Service.Users
{
    [TestFixture]
    public sealed class UserServiceTests
    {
        private Mock<IFileRepository<OAuthUserDataObject>> repository = null!;

        private Mock<ISecretHasher> secretHasher = null!;

        private Mock<ILogger> logger = null!;

        private UserService service = null!;

        [SetUp]
        public void SetUp()
        {
            repository = new();
            secretHasher = new();
            logger = new();
            service = new(repository.Object, BuildSettings(), secretHasher.Object, logger.Object);
        }

        [Test]
        public void GivenMatchingCredentials_WhenAuthenticating_ThenTheUserIsReturned()
        {
            repository.Setup(repository => repository.Find(It.IsAny<System.Func<OAuthUserDataObject, bool>>()))
                .Returns([BuildUserDataObject()]);
            secretHasher.Setup(hasher => hasher.VerifySecret("0bvious-Passphrase", "stored-hash"))
                .Returns(true);

            UserAuthentication authentication = service.Authenticate("solaire_of_astora", "0bvious-Passphrase");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authentication.IsAuthenticated, Is.True);
                Assert.That(authentication.User.Username, Is.EqualTo("solaire_of_astora"));
                Assert.That(authentication.User.Email, Is.EqualTo("solaire@astora.example"));
            }
        }

        [TestCase("0bvious-Passphras", false)]
        [TestCase("", false)]
        public void GivenNonMatchingCredentials_WhenAuthenticating_ThenNoUserIsReturned(
            string candidatePassword,
            bool expected)
        {
            repository.Setup(repository => repository.Find(It.IsAny<System.Func<OAuthUserDataObject, bool>>()))
                .Returns([BuildUserDataObject()]);
            secretHasher.Setup(hasher => hasher.VerifySecret(It.IsAny<string>(), "stored-hash"))
                .Returns(false);

            UserAuthentication authentication = service.Authenticate("solaire_of_astora", candidatePassword);

            Assert.That(authentication.IsAuthenticated, Is.EqualTo(expected));
        }

        [Test]
        public void GivenNoPassword_WhenAuthenticating_ThenNoUserIsReturned()
        {
            repository.Setup(repository => repository.Find(It.IsAny<System.Func<OAuthUserDataObject, bool>>()))
                .Returns([BuildUserDataObject()]);

            UserAuthentication authentication = service.Authenticate("solaire_of_astora", null!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authentication.IsAuthenticated, Is.False);
                Assert.That(authentication.User, Is.Null);
            }

            secretHasher.Verify(hasher => hasher.VerifySecret(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GivenAnUnknownUsername_WhenAuthenticating_ThenNoUserIsReturned()
        {
            repository.Setup(repository => repository.Find(It.IsAny<System.Func<OAuthUserDataObject, bool>>()))
                .Returns([]);

            UserAuthentication authentication = service.Authenticate("grumpy_cat", "0bvious-Passphrase");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authentication.IsAuthenticated, Is.False);
                Assert.That(authentication.User, Is.Null);
            }

            secretHasher.Verify(hasher => hasher.VerifySecret(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GivenSeedUsers_WhenSeeding_ThenMissingAccountsAreAdded()
        {
            repository.Setup(repository => repository.Find(It.IsAny<System.Func<OAuthUserDataObject, bool>>()))
                .Returns([]);

            service.SeedUsers();

            repository.Verify(repository => repository.Add(It.Is<OAuthUserDataObject>(user =>
                user.Username == "solaire_of_astora" &&
                user.PasswordHash == "stored-hash" &&
                user.IsEmailVerified)), Times.Once);
            repository.Verify(repository => repository.SaveChanges(), Times.Once);
        }

        [Test]
        public void GivenAnExistingSeedUser_WhenSeeding_ThenItIsNotDuplicated()
        {
            repository.Setup(repository => repository.Find(It.IsAny<System.Func<OAuthUserDataObject, bool>>()))
                .Returns([BuildUserDataObject()]);

            service.SeedUsers();

            repository.Verify(repository => repository.Add(It.IsAny<OAuthUserDataObject>()), Times.Never);
            repository.Verify(repository => repository.SaveChanges(), Times.Never);
        }

        [TestCase("")]
        [TestCase("   ")]
        public void GivenABlankUsername_WhenSeeding_ThenTheDefinitionIsIgnored(string username)
        {
            OAuthServerSettings settings = BuildSettings();
            settings.SeedUsers = [new() { Username = username, PasswordHash = "stored-hash" }];
            UserService blankService = new(repository.Object, settings, secretHasher.Object, logger.Object);

            blankService.SeedUsers();

            repository.Verify(repository => repository.Add(It.IsAny<OAuthUserDataObject>()), Times.Never);
        }

        private static OAuthUserDataObject BuildUserDataObject()
            => new()
            {
                Id = "user-613",
                Username = "solaire_of_astora",
                PasswordHash = "stored-hash",
                Email = "solaire@astora.example",
                DisplayName = "Solaire of Astora",
                IsEmailVerified = true
            };

        private static OAuthServerSettings BuildSettings()
            => new()
            {
                Issuer = "https://auth.example.com",
                SupportedScopes = ["memories", "openid"],
                AllowedResources = ["https://memory.example.com/mcp"],
                PasswordHashIterations = 1000,
                SeedUsers =
                [
                    new()
                    {
                        Username = "solaire_of_astora",
                        PasswordHash = "stored-hash",
                        Email = "solaire@astora.example",
                        DisplayName = "Solaire of Astora",
                        IsEmailVerified = true
                    }
                ]
            };
    }
}