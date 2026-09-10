using Platform.Operator.API.Repository;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Operators = Platform.Shared.Data.Platform.Entities;

namespace Platform.Operator.API.Test.Repository
{
    // Covers the lookup, validation and list-management methods of PlatformRepository.
    // The money arithmetic lives in OperatorAuditTests.
    //
    // Platform.Operator.API is the operator-facing integration surface: every method here
    // answers an external operator, and the StatusId it returns is the contract that operator
    // switches on. The numbers are undocumented magic values in the production code
    // (Platform.Operator.API/Repository/PlatformRepository.cs), so pinning them is the point:
    // 1000 success, 1001 insufficient balance, 1002 invalid token, 1003 internal error.
    public class PlatformRepositoryTests
    {
        private static Operators.Operator AnOperator(Guid userId, string token, decimal balance = 100m, Guid clientId = default) =>
            new Operators.Operator
            {
                UserId = userId,
                OperatorToken = token,
                ClientId = clientId,
                Balance = balance,
                CurrencyCode = "EUR",
                CreatedDate = DateTime.UtcNow
            };

        #region Authenticate

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Authenticate_MissingToken_ReturnsNull(string token)
        {
            // Null, not a status code. The caller has to null-check this one method, unlike
            // WalletBalance below which answers the same input with 1003.
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Null(await repository.Authenticate(token));
        }

        [Fact]
        public async Task Authenticate_TokenIsNotAGuid_Returns1002()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var response = await repository.Authenticate("not-a-guid");

            Assert.Equal(1002, response.StatusId);
        }

        [Fact]
        public async Task Authenticate_TokenNotFound_Returns1002()
        {
            // A well-formed but unknown token is indistinguishable from a malformed one, on
            // purpose: neither tells the caller whether the token exists.
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var response = await repository.Authenticate(Guid.NewGuid().ToString());

            Assert.Equal(1002, response.StatusId);
        }

        [Fact]
        public async Task Authenticate_KnownToken_ReturnsOperatorDetails()
        {
            var userId = Guid.NewGuid();
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, token, 250.5m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.Authenticate(token);

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(250.5m, response.Balance);
            Assert.Equal("EUR", response.CurrencyCode);
            Assert.Equal(userId.ToString().ToUpper(), response.UserId);
        }

        [Fact]
        public async Task Authenticate_AlwaysReportsLanguageEN()
        {
            // Hardcoded at PlatformRepository.cs:60 and never read from the operator row.
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token));
            context.SaveChanges();

            var repository = new PlatformRepository(context);

            Assert.Equal("EN", (await repository.Authenticate(token)).LanguageCode);
        }

        [Fact]
        public async Task Authenticate_TokenCasingIsIgnored()
        {
            // Both sides are lowercased before comparison, so an operator may send either casing.
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token.ToLowerInvariant()));
            context.SaveChanges();

            var repository = new PlatformRepository(context);

            Assert.Equal(1000, (await repository.Authenticate(token.ToUpperInvariant())).StatusId);
        }

        #endregion

        #region WalletBalance

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task WalletBalance_MissingToken_Returns1003(string token)
        {
            // 1003, where Authenticate returns null for the same input.
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Equal(1003, (await repository.WalletBalance(token)).StatusId);
        }

        [Fact]
        public async Task WalletBalance_TokenIsNotAGuid_Returns1002()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Equal(1002, (await repository.WalletBalance("not-a-guid")).StatusId);
        }

        [Fact]
        public async Task WalletBalance_TokenNotFound_Returns1002()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Equal(1002, (await repository.WalletBalance(Guid.NewGuid().ToString())).StatusId);
        }

        [Fact]
        public async Task WalletBalance_KnownToken_ReturnsBalance()
        {
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 1234.567890123456m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.WalletBalance(token);

            Assert.Equal(1000, response.StatusId);

            // Full decimal(26,12) scale survives the round trip. This is the reason the
            // in-memory provider was chosen over SQLite, which has no native decimal type.
            Assert.Equal(1234.567890123456m, response.Balance);
        }

        #endregion

        #region WalletBalanceOG

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task WalletBalanceOG_MissingUserId_ReturnsS101(string userId)
        {
            // Oriental Gaming uses string codes rather than the numeric StatusId.
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Equal("S-101", (await repository.WalletBalanceOG(userId)).Code);
        }

        [Fact]
        public async Task WalletBalanceOG_UserNotFound_ReturnsS102()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Equal("S-102", (await repository.WalletBalanceOG(Guid.NewGuid().ToString())).Code);
        }

        [Fact]
        public async Task WalletBalanceOG_KnownUser_ReturnsBalanceAsStringAndEchoesPlayerCode()
        {
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 42.25m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.WalletBalanceOG(userId.ToString());

            Assert.Equal("S-100", response.Code);
            Assert.Equal("EUR", response.CurrencyCode);

            // Balance crosses this boundary as a string, and PlayerCode is echoed back
            // verbatim rather than re-read from the row.
            Assert.Equal(42.25m.ToString(), response.Balance);
            Assert.Equal(userId.ToString(), response.PlayerCode);
        }

        #endregion

        #region Negative-test client list

        [Fact]
        public void AddClient_InvalidGuid_ReturnsFalseAndStoresNothing()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.False(repository.AddClient("not-a-guid"));
            Assert.Empty(context.NegativeTest);
        }

        [Fact]
        public void AddClient_NewClient_StoresIt()
        {
            var clientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.True(repository.AddClient(clientId.ToString()));
            Assert.Equal(clientId, Assert.Single(context.NegativeTest).ClientId);
        }

        [Fact]
        public void AddClient_AlreadyPresent_IsIdempotent()
        {
            // Second add returns true without inserting a duplicate. Worth pinning because
            // ClientId is the primary key, so a real insert would throw.
            var clientId = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.True(repository.AddClient(clientId));
            Assert.True(repository.AddClient(clientId));
            Assert.Single(context.NegativeTest);
        }

        [Fact]
        public void RemoveClient_InvalidGuid_ReturnsFalse()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.False(repository.RemoveClient("not-a-guid"));
        }

        [Fact]
        public void RemoveClient_Present_DeletesIt()
        {
            var clientId = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);
            repository.AddClient(clientId);

            Assert.True(repository.RemoveClient(clientId));
            Assert.Empty(context.NegativeTest);
        }

        [Fact]
        public void RemoveClient_Absent_ReturnsTrueAnyway()
        {
            // A valid Guid always reports success, whether or not anything was removed.
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.True(repository.RemoveClient(Guid.NewGuid().ToString()));
        }

        [Fact]
        public void GetClients_Empty_ReturnsEmptyListNotNull()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Empty(repository.GetClients());
        }

        [Fact]
        public void GetClients_ReturnsEveryStoredClientId()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.NegativeTest.AddRange(new NegativeTest { ClientId = first }, new NegativeTest { ClientId = second });
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var clients = repository.GetClients();

            Assert.Equal(2, clients.Count);
            Assert.Contains(first.ToString(), clients);
            Assert.Contains(second.ToString(), clients);
        }

        #endregion

        #region Test config

        [Fact]
        public void AddTestConfig_InvalidGuid_ReturnsFalseAndStoresNothing()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.False(repository.AddTestConfig("not-a-guid", true, 50m, 50m));
            Assert.Empty(context.TestConfig);
        }

        [Fact]
        public void AddTestConfig_NewClient_StoresEveryField()
        {
            var clientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.True(repository.AddTestConfig(clientId.ToString(), true, 60m, 40m));

            var stored = Assert.Single(context.TestConfig);
            Assert.Equal(clientId, stored.ClientId);
            Assert.True(stored.IsFreeBet);
            Assert.Equal(60m, stored.StakeRealPercent);
            Assert.Equal(40m, stored.StakeBonusPercent);
        }

        [Fact]
        public void AddTestConfig_AlreadyPresent_KeepsTheOriginalValues()
        {
            // The second call short-circuits on Find and never updates, so a caller trying to
            // change a split has to remove first. Pinned because "add" reads like an upsert.
            var clientId = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            repository.AddTestConfig(clientId, true, 60m, 40m);
            Assert.True(repository.AddTestConfig(clientId, false, 10m, 90m));

            var stored = Assert.Single(context.TestConfig);
            Assert.True(stored.IsFreeBet);
            Assert.Equal(60m, stored.StakeRealPercent);
        }

        [Fact]
        public void RemoveTestConfig_InvalidGuid_ReturnsFalse()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.False(repository.RemoveTestConfig("not-a-guid"));
        }

        [Fact]
        public void RemoveTestConfig_Present_DeletesIt()
        {
            var clientId = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);
            repository.AddTestConfig(clientId, false, 100m, 0m);

            Assert.True(repository.RemoveTestConfig(clientId));
            Assert.Empty(context.TestConfig);
        }

        [Fact]
        public void GetTestConfig_Empty_ReturnsEmptyListNotNull()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Empty(repository.GetTestConfig());
        }

        [Fact]
        public void GetTestConfig_ReturnsStoredRows()
        {
            var clientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.TestConfig.Add(new TestConfig { ClientId = clientId, IsFreeBet = true, StakeRealPercent = 70m, StakeBonusPercent = 30m });
            context.SaveChanges();

            var repository = new PlatformRepository(context);

            Assert.Equal(clientId, Assert.Single(repository.GetTestConfig()).ClientId);
        }

        #endregion

        #region AddUser

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("XYZ")]
        public async Task AddUser_UnsupportedCurrency_ReturnsUnsupportedCurrencyCode(string currency)
        {
            // Guarded by Enum.IsDefined against CurrencyCode, so an operator cannot create a
            // wallet in a currency the platform does not price.
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var response = await repository.AddUser(new GenerateUserRequestDTO { CurrencyCode = currency, Balance = 10m, ClientId = Guid.NewGuid() });

            Assert.Equal(StatusCode.Unsupported_CurrencyCode, response.StatusCode);
            Assert.Empty(context.Operator);
        }

        [Fact]
        public async Task AddUser_SupportedCurrency_CreatesOperatorWithGeneratedTokenAndUserId()
        {
            var clientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var response = await repository.AddUser(new GenerateUserRequestDTO
            {
                ClientId = clientId,
                Balance = 500m,
                CurrencyCode = CurrencyCode.EUR.ToString()
            });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(clientId, response.Result.ClientId);
            Assert.Equal(500m, response.Result.Balance);
            Assert.NotEqual(Guid.Empty, response.Result.UserId);
            Assert.True(Guid.TryParse(response.Result.OperatorToken, out _));

            var stored = Assert.Single(context.Operator);
            Assert.Equal(response.Result.UserId, stored.UserId);
        }

        #endregion

        #region Fixed responses

        [Fact]
        public async Task FixtureScore_AlwaysReturns1000()
        {
            // The body is an unconditional success with an unreachable catch; there is no
            // score lookup behind it. Pinned so that giving it a real implementation is a
            // visible change rather than a silent one.
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Equal(1000, (await repository.FixtureScore(new FixtureScoreRequestDTO())).StatusId);
        }

        [Fact]
        public async Task CashOutResult_AlwaysReturns1000()
        {
            // Same shape as FixtureScore: acknowledges without doing anything.
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            Assert.Equal(1000, (await repository.CashOutResult(new CashoutResultRequestDTO())).StatusId);
        }

        #endregion
    }
}
