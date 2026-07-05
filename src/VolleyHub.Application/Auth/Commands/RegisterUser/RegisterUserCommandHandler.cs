using MediatR;
using VolleyHub.Application.Auth.Dtos;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Users;

namespace VolleyHub.Application.Auth.Commands.RegisterUser
{
    public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResultDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterUserCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResultDto> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            var email = NormalizeEmail(request.Email);

            var existingUser = await _userRepository.GetByEmailAsync(
                email,
                cancellationToken);

            if (existingUser is not null)
            {
                throw new ArgumentException(
                    "User with this email already exists.",
                    nameof(request.Email));
            }

            var passwordHash = _passwordHasher.Hash(request.Password);

            var user = User.Create(email, passwordHash);

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = _jwtTokenGenerator.GenerateToken(user);

            return new AuthResultDto(
                user.Id,
                user.Email,
                accessToken);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}