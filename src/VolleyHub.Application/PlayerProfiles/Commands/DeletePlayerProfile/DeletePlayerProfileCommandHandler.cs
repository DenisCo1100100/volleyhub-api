using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.DeletePlayerProfile
{
    public sealed class DeletePlayerProfileCommandHandler : IRequestHandler<DeletePlayerProfileCommand>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public DeletePlayerProfileCommandHandler(
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            DeletePlayerProfileCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var playerProfile = await _playerProfileRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), request.Id);
            }

            if (playerProfile.UserId != currentUserId.Value)
            {
                throw new ForbiddenAccessException();
            }

            playerProfile.Delete();

            _playerProfileRepository.Update(playerProfile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}