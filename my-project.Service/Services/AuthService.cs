using AutoMapper;
using Microsoft.Extensions.Logging;
using my_project.Core.Common;
using my_project.Core.DTOs.Auth;
using my_project.Core.DTOs.Users;
using my_project.Core.Entities;
using my_project.Core.Interfaces;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Interfaces.Services;
using my_project.Service.Common;

namespace my_project.Service.Services;

public class AuthService : IAuthService
{
    private static string? _unknownUserHash;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IClock clock,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _clock = clock;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
        {
            return ServiceResult<AuthResponse>.Conflict(ErrorMessages.EmailTaken);
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = _clock.UtcNow,
            // Default notification settings are created in the same save
            NotificationSettings = new NotificationSettings()
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} registered", user.Id);

        return ServiceResult<AuthResponse>.Ok(CreateAuthResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        // A password is verified even when the email is unknown (against a hash of a random value),
        // so the response time does not reveal which emails exist either
        var passwordIsValid = _passwordHasher.Verify(user?.PasswordHash ?? GetUnknownUserHash(), request.Password);

        // Same message for all three cases, so the response does not reveal which emails exist
        if (user is null || !passwordIsValid || !user.IsActive)
        {
            _logger.LogInformation("Failed login attempt");
            return ServiceResult<AuthResponse>.Unauthorized(ErrorMessages.InvalidCredentials);
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return ServiceResult<AuthResponse>.Ok(CreateAuthResponse(user));
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _tokenService.CreateToken(user);

        return new AuthResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = _mapper.Map<UserDto>(user)
        };
    }

    // Computed once per process; no user can have this password
    private string GetUnknownUserHash()
    {
        return _unknownUserHash ??= _passwordHasher.Hash(Guid.NewGuid().ToString());
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
