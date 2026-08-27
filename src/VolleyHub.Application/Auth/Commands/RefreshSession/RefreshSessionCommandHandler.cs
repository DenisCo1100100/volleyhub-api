using MediatR;
using VolleyHub.Application.Auth.Dtos;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Auth;

namespace VolleyHub.Application.Auth.Commands.RefreshSession
{
    public sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, AuthResultDto>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshSessionCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            IRefreshTokenGenerator refreshTokenGenerator,
            IJwtTokenGenerator jwtTokenGenerator,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _refreshTokenGenerator = refreshTokenGenerator;
            _jwtTokenGenerator = jwtTokenGenerator;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResultDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new UnauthorizedException("Refresh token is missing.");
            }

            var tokenHash = _refreshTokenGenerator.HashToken(request.RefreshToken);

            var currentRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

            if (currentRefreshToken is null)
            {
                throw new UnauthorizedException("Refresh token is invalid.");
            }

            var now = _dateTimeProvider.UtcNow;

            if (currentRefreshToken.RevokedAt is not null)
            {
                if (currentRefreshToken.IsRotated)
                {
                    await RevokeReplacementChainAsync(
                        currentRefreshToken,
                        now,
                        cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    throw new UnauthorizedException("Refresh token reuse detected.");
                }

                throw new UnauthorizedException("Refresh token has been revoked.");
            }

            if (currentRefreshToken.IsExpired(now))
            {
                throw new UnauthorizedException("Refresh token has expired.");
            }

            var user = await _userRepository.GetByIdAsync(
                currentRefreshToken.UserId,
                cancellationToken);

            if (user is null)
            {
                throw new UnauthorizedException("Refresh token is invalid.");
            }

            var generatedRefreshToken = _refreshTokenGenerator.Generate();

            var replacementRefreshToken = RefreshToken.Create(
                user.Id,
                generatedRefreshToken.TokenHash,
                generatedRefreshToken.ExpiresAt);

            currentRefreshToken.Rotate(
                replacementRefreshToken.Id,
                now);

            _refreshTokenRepository.Update(currentRefreshToken);

            await _refreshTokenRepository.AddAsync(
                replacementRefreshToken,
                cancellationToken);

            await _refreshTokenRepository.RemoveExpiredAsync(
                now,
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

        private async Task RevokeReplacementChainAsync(RefreshToken refreshToken, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        {
            var replacementTokenId = refreshToken.ReplacedByTokenId;

            while (replacementTokenId is not null)
            {
                var replacementToken = await _refreshTokenRepository.GetByIdAsync(
                    replacementTokenId.Value,
                    cancellationToken);

                if (replacementToken is null)
                {
                    return;
                }

                if (replacementToken.RevokedAt is null)
                {
                    replacementToken.Revoke(revokedAt);
                    _refreshTokenRepository.Update(replacementToken);

                    return;
                }

                replacementTokenId = replacementToken.ReplacedByTokenId;
            }
        }
    }
}