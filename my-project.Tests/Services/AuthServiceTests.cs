using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using my_project.Core.Common;
using my_project.Core.DTOs.Auth;
using my_project.Core.Entities;
using my_project.Core.Enums;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Repositories;
using my_project.Service.Options;
using my_project.Service.Security;
using my_project.Service.Services;
using my_project.Tests.Helpers;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace my_project.Tests.Services;

// The real PasswordHasher and JwtTokenService; only the database is mocked
public class AuthServiceTests
{
    private const string Password = "Secret123!";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly PasswordHasher _hasher = new();
    private readonly TestClock _clock = new();
    private readonly JwtTokenService _tokens;

    public AuthServiceTests()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.Users).Returns(_users.Object);

        _tokens = new JwtTokenService(MsOptions.Create(new JwtSettings
        {
            Issuer = "test", Audience = "test-client", ExpiryMinutes = 60,
            Key = "a-test-only-signing-key-that-is-long-enough-1234567890"
        }), _clock);
    }

    private AuthService CreateService() =>
        new(_unitOfWork.Object, _hasher, _tokens, _clock, TestMapper.Create(), NullLogger<AuthService>.Instance);

    private User StoredUser(bool isActive = true, SystemRole role = SystemRole.User) => new()
    {
        Id = 5, FullName = "נועה", Email = "noa@test.local", PasswordHash = _hasher.Hash(Password), IsActive = isActive, Role = role
    };

    [Fact]
    public async Task Register_ExistingEmail_ReturnsConflict()
    {
        _users.Setup(users => users.EmailExistsAsync("noa@test.local", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().RegisterAsync(new RegisterRequest { FullName = "נועה", Email = "  NOA@test.local ", Password = Password });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Register_NewUser_StoresHashNotPassword_AndReturnsToken()
    {
        User? added = null;
        _users.Setup(users => users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback((User user, CancellationToken _) => added = user);

        var result = await CreateService().RegisterAsync(new RegisterRequest { FullName = " נועה ", Email = "Noa@Test.local", Password = Password });

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal("noa@test.local", added.Email);
        Assert.NotEqual(Password, added.PasswordHash);
        Assert.True(_hasher.Verify(added.PasswordHash, Password));
        Assert.Equal(SystemRole.User, added.Role);
        Assert.NotNull(added.NotificationSettings);
        Assert.False(string.IsNullOrEmpty(result.Data!.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        _users.Setup(users => users.GetByEmailAsync("noa@test.local", It.IsAny<CancellationToken>())).ReturnsAsync(StoredUser());

        var result = await CreateService().LoginAsync(new LoginRequest { Email = "noa@test.local", Password = "wrong-password" });

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsTheSameUnauthorized()
    {
        var result = await CreateService().LoginAsync(new LoginRequest { Email = "nobody@test.local", Password = Password });

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_BlockedUser_ReturnsUnauthorized()
    {
        _users.Setup(users => users.GetByEmailAsync("noa@test.local", It.IsAny<CancellationToken>())).ReturnsAsync(StoredUser(isActive: false));

        var result = await CreateService().LoginAsync(new LoginRequest { Email = "noa@test.local", Password = Password });

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_SystemAdmin_TokenCarriesTheRoleAndUserId()
    {
        _users.Setup(users => users.GetByEmailAsync("noa@test.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoredUser(role: SystemRole.SystemAdmin));

        var result = await CreateService().LoginAsync(new LoginRequest { Email = "noa@test.local", Password = Password });

        Assert.True(result.IsSuccess);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Data!.Token);
        Assert.Contains(token.Claims, claim => claim.Type is ClaimTypes.Role or "role" && claim.Value == nameof(SystemRole.SystemAdmin));
        Assert.Contains(token.Claims, claim => claim.Value == "5");
        Assert.Equal(_clock.UtcNow.AddMinutes(60), result.Data.ExpiresAt);
    }

    [Fact]
    public void JwtTokenService_ShortKey_IsRejected()
    {
        var settings = MsOptions.Create(new JwtSettings { Issuer = "i", Audience = "a", Key = "too-short" });

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(settings, _clock));
    }
}
