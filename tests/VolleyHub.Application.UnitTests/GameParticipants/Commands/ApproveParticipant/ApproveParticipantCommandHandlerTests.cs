using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.ApproveParticipant;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.ApproveParticipant
{
    public sealed class ApproveParticipantCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldApproveParticipantAndSaveChanges_WhenCurrentUserIsOrganizer()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);

            var joinedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var approvedAt = DateTimeOffset.UtcNow;

            var game = CreateGame(organizerProfile.Id);
            var participant = CreatePendingParticipant(game.Id, joinedAt);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
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

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant> { participant });

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(approvedAt);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participant.Id);

            await handler.Handle(command, CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            participant.ApprovedAt.Should().Be(approvedAt);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldMarkGameAsFull_WhenApprovedParticipantReachesMaxPlayers()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);

            var joinedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var approvedAt = DateTimeOffset.UtcNow;

            var game = CreateGame(organizerProfile.Id, maxPlayers: 2);
            var pendingParticipant = CreatePendingParticipant(game.Id, joinedAt);
            var approvedParticipant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    pendingParticipant.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingParticipant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>
                {
                    pendingParticipant,
                    approvedParticipant
                });

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(approvedAt);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(pendingParticipant.Id);

            await handler.Handle(command, CancellationToken.None);

            pendingParticipant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            game.Status.Should().Be(GameStatus.Full);

            gameRepositoryMock.Verify(
                repository => repository.Update(game),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(pendingParticipant),
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

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(Guid.NewGuid());

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
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile: null);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(Guid.NewGuid());

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
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentOrganizer(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                organizerProfile);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(Guid.NewGuid());

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
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
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

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participantId);

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
            var participant = CreatePendingParticipant(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-10));

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
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

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participant.Id);

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
            var participant = CreatePendingParticipant(game.Id, DateTimeOffset.UtcNow.AddMinutes(-10));

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
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

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participant.Id);

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
        public async Task Handle_ShouldThrowBusinessRuleException_WhenGameIsFull()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);

            var joinedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var game = CreateGame(organizerProfile.Id, maxPlayers: 2);
            game.MarkAsFull();

            var participant = CreatePendingParticipant(game.Id, joinedAt);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
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

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participant.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();

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

        private static Game CreateGame(
            Guid organizerId,
            int maxPlayers = 12)
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            return Game.Create(
                organizerId: organizerId,
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: maxPlayers,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.ApprovalRequired,
                description: "Evening volleyball game");
        }

        private static GameParticipant CreatePendingParticipant(
            Guid gameId,
            DateTimeOffset joinedAt)
        {
            return GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: joinedAt,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateApprovedParticipant(Guid gameId)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
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