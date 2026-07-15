using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.CreatePlayerProfile
{
    public sealed class CreatePlayerProfileCommandHandler : IRequestHandler<CreatePlayerProfileCommand, Guid>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePlayerProfileCommandHandler(
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(
            CreatePlayerProfileCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var existingProfile = await _playerProfileRepository.GetByUserIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (existingProfile is not null)
            {
                throw new InvalidOperationException("User already has a player profile.");
            }

            var playerProfile = PlayerProfile.Create(
                currentUserId.Value,
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