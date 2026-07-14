using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.UpdatePlayerProfile
{
    public sealed class UpdatePlayerProfileCommandHandler : IRequestHandler<UpdatePlayerProfileCommand>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePlayerProfileCommandHandler(
            IPlayerProfileRepository playerProfileRepository,
            IUnitOfWork unitOfWork)
        {
            _playerProfileRepository = playerProfileRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            UpdatePlayerProfileCommand request,
            CancellationToken cancellationToken)
        {
            var playerProfile = await _playerProfileRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), request.Id);
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