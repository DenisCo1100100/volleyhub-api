using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.JoinGame;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.JoinGame
{
    public sealed class JoinGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldAddApprovedParticipant_WhenGameHasOpenJoinPolicy()
        {
            var now = DateTimeOffset.UtcNow;
            var game = CreateGame(joinPolicy: GameJoinPolicy.Open, pricePerPlayer: 0);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            GameParticipant? addedParticipant = null;

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    It.IsAny<Guid>(),
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
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(
                GameId: game.Id,
                PlayerProfileId: Guid.NewGuid());

            var participantId = await handler.Handle(command, CancellationToken.None);

            participantId.Should().NotBeEmpty();

            addedParticipant.Should().NotBeNull();
            addedParticipant!.Id.Should().Be(participantId);
            addedParticipant.GameId.Should().Be(game.Id);
            addedParticipant.PlayerProfileId.Should().Be(command.PlayerProfileId);
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
            var game = CreateGame(joinPolicy: GameJoinPolicy.ApprovalRequired, pricePerPlayer: 15);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            GameParticipant? addedParticipant = null;

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    It.IsAny<Guid>(),
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
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(
                GameId: game.Id,
                PlayerProfileId: Guid.NewGuid());

            var participantId = await handler.Handle(command, CancellationToken.None);

            participantId.Should().NotBeEmpty();

            addedParticipant.Should().NotBeNull();
            addedParticipant!.JoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);
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
            var game = CreateGame(
                joinPolicy: GameJoinPolicy.Open,
                pricePerPlayer: 0,
                maxPlayers: 2);

            var existingParticipant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    It.IsAny<Guid>(),
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
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(
                GameId: game.Id,
                PlayerProfileId: Guid.NewGuid());

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
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var gameId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(
                GameId: gameId,
                PlayerProfileId: Guid.NewGuid());

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
        public async Task Handle_ShouldThrowInvalidOperationException_WhenGameIsInviteOnly()
        {
            var game = CreateGame(joinPolicy: GameJoinPolicy.InviteOnly, pricePerPlayer: 0);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(
                GameId: game.Id,
                PlayerProfileId: Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

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
        public async Task Handle_ShouldThrowInvalidOperationException_WhenPlayerAlreadyJoinedGame()
        {
            var game = CreateGame(joinPolicy: GameJoinPolicy.Open, pricePerPlayer: 0);
            var playerProfileId = Guid.NewGuid();
            var existingParticipant = GameParticipant.JoinOpenGame(
                game.Id,
                playerProfileId,
                DateTimeOffset.UtcNow,
                GameParticipantOfflinePaymentStatus.NotRequired);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfileId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingParticipant);

            var handler = new JoinGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new JoinGameCommand(
                GameId: game.Id,
                PlayerProfileId: playerProfileId);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<GameParticipant>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
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
    }
}