using VideoProcessorX.Application.Common;
using VideoProcessorX.Domain.Entities;
using VideoProcessorX.Domain.Interfaces;

namespace VideoProcessorX.Application.Services
{
    public class LoginUserService
    {
        private readonly IUserRepository _userRepository;

        public LoginUserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<User>> LoginAsync(string username, string password)
        {
            // Busca usuário
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return Result<User>.Failure("Invalid username or password");
            }

            // Verifica a senha
            bool validPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if (!validPassword)
            {
                return Result<User>.Failure("Invalid username or password");
            }

            // Retorna user
            return Result<User>.Success(user);
        }
    }
}
