using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.JoinGame;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.JoinGame
{
    public sealed class JoinGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldAddApprovedParticipant_WhenGameHasOpenJoinPolicy()
        {
            var now = DateTimeOffset.UtcNow;
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(joinPolicy: GameJoinPolicy.Open, pricePerPlayer: 0);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            GameParticipant? addedParticipant = null;

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>());

            gameParticipantRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()))
                .Callback<GameParticipant, CancellationToken>((participant, _) => addedParticipant = participant)
                .Returns(Task.CompletedTask);

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(now);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(game.Id);

            var participantId = await handler.Handle(command, CancellationToken.None);

            participantId.Should().NotBeEmpty();

            addedParticipant.Should().NotBeNull();
            addedParticipant!.Id.Should().Be(participantId);
            addedParticipant.GameId.Should().Be(game.Id);
            addedParticipant.PlayerProfileId.Should().Be(playerProfile.Id);
            addedParticipant.JoinedAt.Should().Be(now);
            addedParticipant.ApprovedAt.Should().Be(now);
            addedParticipant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            addedParticipant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            addedParticipant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.NotRequired);

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldAddPendingParticipant_WhenGameRequiresApproval()
        {
            var now = DateTimeOffset.UtcNow;
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(joinPolicy: GameJoinPolicy.ApprovalRequired, pricePerPlayer: 15);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            GameParticipant? addedParticipant = null;

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>());

            gameParticipantRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()))
                .Callback<GameParticipant, CancellationToken>((participant, _) => addedParticipant = participant)
                .Returns(Task.CompletedTask);

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(now);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(game.Id);

            var participantId = await handler.Handle(command, CancellationToken.None);

            participantId.Should().NotBeEmpty();

            addedParticipant.Should().NotBeNull();
            addedParticipant!.GameId.Should().Be(game.Id);
            addedParticipant.PlayerProfileId.Should().Be(playerProfile.Id);
            addedParticipant.JoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);
            addedParticipant.ApprovedAt.Should().BeNull();
            addedParticipant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldMarkGameAsFull_WhenOpenGameReachesMaxPlayers()
        {
            var now = DateTimeOffset.UtcNow;
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);

            var game = CreateGame(
                joinPolicy: GameJoinPolicy.Open,
                pricePerPlayer: 0,
                maxPlayers: 2);

            var existingParticipant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant> { existingParticipant });

            gameParticipantRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(now);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(game.Id);

            await handler.Handle(command, CancellationToken.None);

            game.Status.Should().Be(GameStatus.Full);

            gameRepositoryMock.Verify(
                repository => repository.Update(game),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserDoesNotHavePlayerProfile()
        {
            var currentUserId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile: null);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserPlayerProfileIsDeleted()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            playerProfile.Delete();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var gameId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(gameId);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessRuleException_WhenGameIsInviteOnly()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(joinPolicy: GameJoinPolicy.InviteOnly, pricePerPlayer: 0);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(game.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessRuleException_WhenPlayerAlreadyJoinedGame()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(joinPolicy: GameJoinPolicy.Open, pricePerPlayer: 0);

            var existingParticipant = GameParticipant.JoinOpenGame(
                game.Id,
                playerProfile.Id,
                DateTimeOffset.UtcNow,
                GameParticipantOfflinePaymentStatus.NotRequired);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingParticipant);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(game.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static void SetupCurrentUser(
            Mock<ICurrentUserService> currentUserServiceMock,
            Mock<IPlayerProfileRepository> playerProfileRepositoryMock,
            Guid currentUserId,
            PlayerProfile? playerProfile)
        {
            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);
        }

        private static Game CreateGame(
            GameJoinPolicy joinPolicy,
            decimal pricePerPlayer,
            int maxPlayers = 12)
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            return Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: maxPlayers,
                pricePerPlayer: pricePerPlayer,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: joinPolicy,
                description: "Evening volleyball game");
        }

        private static GameParticipant CreateApprovedParticipant(Guid gameId)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);
        }

        private static PlayerProfile CreatePlayerProfile(Guid userId)
        {
            return PlayerProfile.Create(
                userId: userId,
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "I like volleyball.");
        }
    }
}