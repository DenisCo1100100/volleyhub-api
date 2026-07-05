using MediatR;
using VolleyHub.Application.Auth.Dtos;
using VolleyHub.Application.Common.Interfaces;

namespace VolleyHub.Application.Auth.Commands.LoginUser
{
    public sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResultDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LoginUserCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResultDto> Handle(
            LoginUserCommand request,
            CancellationToken cancellationToken)
        {
            var email = NormalizeEmail(request.Email);

            var user = await _userRepository.GetByEmailAsync(
                email,
                cancellationToken);

            if (user is null)
            {
                throw new ArgumentException("Invalid email or password.");
            }

            var isPasswordValid = _passwordHasher.Verify(
                request.Password,
                user.PasswordHash);

            if (!isPasswordValid)
            {
                throw new ArgumentException("Invalid email or password.");
            }

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