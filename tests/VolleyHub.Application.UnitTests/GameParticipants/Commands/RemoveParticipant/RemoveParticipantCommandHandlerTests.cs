using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.RemoveParticipant;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.RemoveParticipant
{
    public sealed class RemoveParticipantCommandHandlerTests
    {
        private static readonly DateTimeOffset Now =
            new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Handle_ShouldRemoveParticipantAndSaveChanges_WhenCurrentUserIsOrganizer()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(organizerProfile.Id, Now.AddDays(2));
            var participant = CreateApprovedParticipant(
                game.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                organizerProfile,
                game,
                participant);

            await context.Handler.Handle(
                new RemoveParticipantCommand(participant.Id),
                CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Removed);
            participant.RemovedAt.Should().Be(Now);
            participant.CancelledAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();

            context.GameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
                Times.Once);

            context.UnitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReopenGame_WhenApprovedParticipantIsRemovedFromFullGame()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(organizerProfile.Id, Now.AddDays(2));
            game.MarkAsFull();

            var participant = CreateApprovedParticipant(
                game.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                organizerProfile,
                game,
                participant);

            await context.Handler.Handle(
                new RemoveParticipantCommand(participant.Id),
                CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Removed);
            game.Status.Should().Be(GameStatus.Open);

            context.GameRepositoryMock.Verify(
                repository => repository.Update(game),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessRuleException_WhenGameHasStarted()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(
                organizerProfile.Id,
                Now.AddMinutes(-1));

            var participant = CreateApprovedParticipant(
                game.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                organizerProfile,
                game,
                participant);

            Func<Task> act = async () => await context.Handler.Handle(
                new RemoveParticipantCommand(participant.Id),
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

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RemoveParticipantCommand(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
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

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RemoveParticipantCommand(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserPlayerProfileIsDeleted()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            organizerProfile.Delete();

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
                .ReturnsAsync(organizerProfile);

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RemoveParticipantCommand(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenParticipantDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var participantId = Guid.NewGuid();

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
                .ReturnsAsync(organizerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participantId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RemoveParticipantCommand(participantId),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);

            var participant = CreateApprovedParticipant(
                Guid.NewGuid(),
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
                .ReturnsAsync(organizerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.GameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RemoveParticipantCommand(participant.Id),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenCurrentUserIsNotOrganizer()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserProfile = CreatePlayerProfile(currentUserId);

            var game = CreateGame(
                organizerId: Guid.NewGuid(),
                startsAt: Now.AddDays(2));

            var participant = CreateApprovedParticipant(
                game.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                currentUserProfile,
                game,
                participant);

            Func<Task> act = async () => await context.Handler.Handle(
                new RemoveParticipantCommand(participant.Id),
                CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();

            context.GameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessRuleException_WhenParticipantIsRejected()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);

            var game = CreateGame(
                organizerProfile.Id,
                Now.AddDays(2));

            var participant = CreateRejectedParticipant(
                game.Id,
                Now.AddDays(-1));

            var context = CreateContext(
                currentUserId,
                organizerProfile,
                game,
                participant);

            Func<Task> act = async () => await context.Handler.Handle(
                new RemoveParticipantCommand(participant.Id),
                CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();

            context.GameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);
        }

        private static TestContext CreateContext(
            Guid currentUserId,
            PlayerProfile organizerProfile,
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
                .ReturnsAsync(organizerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.Id,
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

            var handler = new RemoveParticipantCommandHandler(
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

        private static Game CreateGame(
            Guid organizerId,
            DateTimeOffset startsAt)
        {
            return Game.Create(
                organizerId: organizerId,
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
            DateTimeOffset joinedAt)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: joinedAt,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateRejectedParticipant(
            Guid gameId,
            DateTimeOffset joinedAt)
        {
            var participant = GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: joinedAt,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

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
            RemoveParticipantCommandHandler Handler,
            Mock<IGameRepository> GameRepositoryMock,
            Mock<IGameParticipantRepository> GameParticipantRepositoryMock,
            Mock<IUnitOfWork> UnitOfWorkMock);
    }
}