using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Courts.Commands.CreateCourt
{
    public sealed class CreateCourtCommandHandler
        : IRequestHandler<CreateCourtCommand, Guid>
    {
        private readonly ICourtRepository _courtRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCourtCommandHandler(
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

        public async Task<Guid> Handle(
            CreateCourtCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var ownerProfile =
                await _playerProfileRepository.GetByUserIdAsync(
                    currentUserId.Value,
                    cancellationToken);

            if (ownerProfile is null || ownerProfile.IsDeleted)
            {
                throw new NotFoundException(
                    nameof(PlayerProfile),
                    currentUserId.Value);
            }

            var court = Court.Create(
                ownerProfile.Id,
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.SurfaceType,
                request.IsIndoor,
                request.Description);

            await _courtRepository.AddAsync(
                court,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return court.Id;
        }
    }
}