using MediatR;
using VolleyHub.Application.Common.Interfaces;

namespace VolleyHub.Application.Auth.Commands.Logout
{
    public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public LogoutCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IRefreshTokenGenerator refreshTokenGenerator,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _refreshTokenGenerator = refreshTokenGenerator;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return;
            }

            var tokenHash = _refreshTokenGenerator.HashToken(request.RefreshToken);

            var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (refreshToken is null || refreshToken.RevokedAt is not null)
            {
                return;
            }

            refreshToken.Revoke(_dateTimeProvider.UtcNow);

            _refreshTokenRepository.Update(refreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}