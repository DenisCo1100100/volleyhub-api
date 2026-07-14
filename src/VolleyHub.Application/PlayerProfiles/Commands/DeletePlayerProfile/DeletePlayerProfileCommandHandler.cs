using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.DeletePlayerProfile
{
    public sealed class DeletePlayerProfileCommandHandler : IRequestHandler<DeletePlayerProfileCommand>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeletePlayerProfileCommandHandler(
            IPlayerProfileRepository playerProfileRepository,
            IUnitOfWork unitOfWork)
        {
            _playerProfileRepository = playerProfileRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            DeletePlayerProfileCommand request,
            CancellationToken cancellationToken)
        {
            var playerProfile = await _playerProfileRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), request.Id);
            }

            playerProfile.Delete();

            _playerProfileRepository.Update(playerProfile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}