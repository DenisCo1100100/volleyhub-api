using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.LeaveGame;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.LeaveGame
{
    public sealed class LeaveGameCommandHandlerTests
    {
        private static readonly DateTimeOffset Now =
            new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Handle_ShouldCancelApprovedParticipantAndSaveChanges()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(Now.AddDays(2));
            var participant = CreateApprovedParticipant(
                game.Id,
                playerProfile.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                playerProfile,
                game,
                participant);

            await context.Handler.Handle(
                new LeaveGameCommand(game.Id),
                CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.CancelledAt.Should().Be(Now);
            participant.CancellationType.Should().Be(
                GameParticipantCancellationType.OnTime);

            context.GameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
                Times.Once);

            context.UnitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldWithdrawPendingRequestWithoutCancellationMetadata()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(Now.AddDays(2));
            var participant = CreatePendingParticipant(
                game.Id,
                playerProfile.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                playerProfile,
                game,
                participant);

            await context.Handler.Handle(
                new LeaveGameCommand(game.Id),
                CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.CancelledAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();

            context.GameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
                Times.Never);

            context.GameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReopenGame_WhenApprovedParticipantLeavesFullGame()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(Now.AddDays(2));
            game.MarkAsFull();

            var participant = CreateApprovedParticipant(
                game.Id,
                playerProfile.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                playerProfile,
                game,
                participant);

            await context.Handler.Handle(
                new LeaveGameCommand(game.Id),
                CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            game.Status.Should().Be(GameStatus.Open);

            context.GameRepositoryMock.Verify(
                repository => repository.Update(game),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldNotReopenGame_WhenPendingParticipantLeavesFullGame()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(Now.AddDays(2));
            game.MarkAsFull();

            var participant = CreatePendingParticipant(
                game.Id,
                playerProfile.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                playerProfile,
                game,
                participant);

            await context.Handler.Handle(
                new LeaveGameCommand(game.Id),
                CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            game.Status.Should().Be(GameStatus.Full);

            context.GameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldCreateLateCancellation_WhenLessThanTwentyFourHoursRemain()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(Now.AddHours(6));

            var participant = CreateApprovedParticipant(
                game.Id,
                playerProfile.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                playerProfile,
                game,
                participant);

            await context.Handler.Handle(
                new LeaveGameCommand(game.Id),
                CancellationToken.None);

            participant.CancelledAt.Should().Be(Now);
            participant.CancellationType.Should().Be(
                GameParticipantCancellationType.Late);
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessRuleException_WhenGameHasStarted()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(Now.AddMinutes(-1));

            var participant = CreateApprovedParticipant(
                game.Id,
                playerProfile.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                playerProfile,
                game,
                participant);

            Func<Task> act = async () => await context.Handler.Handle(
                new LeaveGameCommand(game.Id),
                CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();

            context.GameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            context.UnitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock =
                new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new LeaveGameCommand(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserDoesNotHavePlayerProfile()
        {
            var currentUserId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock =
                new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new LeaveGameCommand(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameAndPlayerProfileIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserPlayerProfileIsDeleted()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            playerProfile.Delete();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock =
                new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new LeaveGameCommand(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenParticipantDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var gameId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock =
                new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    gameId,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new LeaveGameCommand(gameId),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);

            var participant = CreateApprovedParticipant(
                Guid.NewGuid(),
                playerProfile.Id,
                Now.AddDays(-1));

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock =
                new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    participant.GameId,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.GameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new LeaveGameCommand(participant.GameId),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessRuleException_WhenParticipantIsRejected()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(Now.AddDays(2));

            var participant = CreateRejectedParticipant(
                game.Id,
                playerProfile.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                playerProfile,
                game,
                participant);

            Func<Task> act = async () => await context.Handler.Handle(
                new LeaveGameCommand(game.Id),
                CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();

            context.GameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);
        }

        private static TestContext CreateContext(
            Guid currentUserId,
            PlayerProfile playerProfile,
            Game game,
            GameParticipant participant)
        {
            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock =
                new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(Now);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            return new TestContext(
                handler,
                gameRepositoryMock,
                gameParticipantRepositoryMock,
                unitOfWorkMock);
        }

        private static Game CreateGame(DateTimeOffset startsAt)
        {
            return Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: 12,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.Open,
                description: "Evening volleyball game");
        }

        private static GameParticipant CreateApprovedParticipant(
            Guid gameId,
            Guid playerProfileId,
            DateTimeOffset joinedAt)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: playerProfileId,
                joinedAt: joinedAt,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreatePendingParticipant(
            Guid gameId,
            Guid playerProfileId,
            DateTimeOffset joinedAt)
        {
            return GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: playerProfileId,
                joinedAt: joinedAt,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateRejectedParticipant(
            Guid gameId,
            Guid playerProfileId,
            DateTimeOffset joinedAt)
        {
            var participant = CreatePendingParticipant(
                gameId,
                playerProfileId,
                joinedAt);

            participant.Reject();

            return participant;
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

        private sealed record TestContext(
            LeaveGameCommandHandler Handler,
            Mock<IGameRepository> GameRepositoryMock,
            Mock<IGameParticipantRepository> GameParticipantRepositoryMock,
            Mock<IUnitOfWork> UnitOfWorkMock);
    }
}