using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Courts.Commands.UpdateCourt
{
    public sealed class UpdateCourtCommandHandler
        : IRequestHandler<UpdateCourtCommand>
    {
        private readonly ICourtRepository _courtRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCourtCommandHandler(
            ICourtRepository courtRepository,
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _courtRepository = courtRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            UpdateCourtCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var court = await _courtRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (court is null || court.IsDeleted)
            {
                throw new NotFoundException(
                    nameof(Court),
                    request.Id);
            }

            var currentPlayerProfile =
                await _playerProfileRepository.GetByUserIdAsync(
                    currentUserId.Value,
                    cancellationToken);

            if (currentPlayerProfile is null
                || currentPlayerProfile.IsDeleted)
            {
                throw new NotFoundException(
                    nameof(PlayerProfile),
                    currentUserId.Value);
            }

            if (!court.IsOwnedBy(currentPlayerProfile.Id))
            {
                throw new ForbiddenAccessException();
            }

            court.Update(
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.SurfaceType,
                request.IsIndoor,
                request.Description);

            _courtRepository.Update(court);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
    }
}