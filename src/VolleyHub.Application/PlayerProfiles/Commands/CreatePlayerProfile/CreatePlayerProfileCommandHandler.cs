using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.CreatePlayerProfile
{
    public sealed class CreatePlayerProfileCommandHandler : IRequestHandler<CreatePlayerProfileCommand, Guid>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePlayerProfileCommandHandler(
            IPlayerProfileRepository playerProfileRepository,
            IUnitOfWork unitOfWork)
        {
            _playerProfileRepository = playerProfileRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(
            CreatePlayerProfileCommand request,
            CancellationToken cancellationToken)
        {
            var existingProfile = await _playerProfileRepository.GetByUserIdAsync(
                request.UserId,
                cancellationToken);

            if (existingProfile is not null)
            {
                throw new InvalidOperationException("User already has a player profile.");
            }

            var playerProfile = PlayerProfile.Create(
                request.UserId,
                request.DisplayName,
                request.SkillLevel,
                request.City,
                request.Bio);

            await _playerProfileRepository.AddAsync(playerProfile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return playerProfile.Id;
        }
    }
}