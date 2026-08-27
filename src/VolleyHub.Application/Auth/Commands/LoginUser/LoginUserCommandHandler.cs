using MediatR;
using VolleyHub.Application.Auth.Dtos;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Auth;

namespace VolleyHub.Application.Auth.Commands.LoginUser
{
    public sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResultDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public LoginUserCommandHandler(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IRefreshTokenGenerator refreshTokenGenerator,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var email = NormalizeEmail(request.Email);

            var user = await _userRepository.GetByEmailAsync(
                email,
                cancellationToken);

            if (user is null)
            {
                throw new ArgumentException("Invalid email or password.");
            }

            var isPasswordValid = _passwordHasher.Verify(
                request.Password,
                user.PasswordHash);

            if (!isPasswordValid)
            {
                throw new ArgumentException("Invalid email or password.");
            }

            var now = _dateTimeProvider.UtcNow;

            await _refreshTokenRepository.RemoveExpiredAsync(
                now,
                cancellationToken);

            var generatedRefreshToken = _refreshTokenGenerator.Generate();

            var refreshToken = RefreshToken.Create(
                user.Id,
                generatedRefreshToken.TokenHash,
                generatedRefreshToken.ExpiresAt);

            await _refreshTokenRepository.AddAsync(
                refreshToken,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = _jwtTokenGenerator.GenerateToken(user);

            return new AuthResultDto(
                user.Id,
                user.Email,
                accessToken,
                generatedRefreshToken.Token,
                generatedRefreshToken.ExpiresAt);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}