using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.RemoveParticipant;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.RemoveParticipant
{
    public sealed class RemoveParticipantCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCancelParticipantAndSaveChanges_WhenCurrentUserIsOrganizer()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(organizerProfile.Id);
            var participant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile);

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

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(participant.Id);

            await handler.Handle(command, CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReopenGame_WhenApprovedParticipantIsRemovedFromFullGame()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(organizerProfile.Id);
            game.MarkAsFull();

            var participant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile);

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

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(participant.Id);

            await handler.Handle(command, CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            game.Status.Should().Be(GameStatus.Open);

            gameRepositoryMock.Verify(
                repository => repository.Update(game),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
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
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
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
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile: null);

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserPlayerProfileIsDeleted()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            organizerProfile.Delete();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile);

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenParticipantDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var participantId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile);

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
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(participantId);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var participant = CreateApprovedParticipant(Guid.NewGuid());

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile);

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
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(participant.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenCurrentUserIsNotOrganizer()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserProfile = CreatePlayerProfile(currentUserId);

            var game = CreateGame(organizerId: Guid.NewGuid());
            var participant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                currentUserProfile);

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

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(participant.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenParticipantIsRejected()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(organizerProfile.Id);
            var participant = CreateRejectedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile);

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

            var handler = new RemoveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new RemoveParticipantCommand(participant.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static void SetupCurrentOrganizer(
            Mock<ICurrentUserService> currentUserServiceMock,
            Mock<IPlayerProfileRepository> playerProfileRepositoryMock,
            Guid currentUserId,
            PlayerProfile? organizerProfile)
        {
            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(organizerProfile);
        }

        private static Game CreateGame(Guid organizerId)
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

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

        private static GameParticipant CreateApprovedParticipant(Guid gameId)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateRejectedParticipant(Guid gameId)
        {
            var participant = GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
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
    }
}