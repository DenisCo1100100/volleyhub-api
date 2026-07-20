using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.UpdateCurrentPlayerProfile
{
    public sealed class UpdateCurrentPlayerProfileCommandHandler
        : IRequestHandler<UpdateCurrentPlayerProfileCommand>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCurrentPlayerProfileCommandHandler(
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            UpdateCurrentPlayerProfileCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var playerProfile = await _playerProfileRepository.GetByUserIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), currentUserId.Value);
            }

            playerProfile.UpdateDetails(
                request.DisplayName,
                request.SkillLevel,
                request.City,
                request.Bio);

            _playerProfileRepository.Update(playerProfile);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}