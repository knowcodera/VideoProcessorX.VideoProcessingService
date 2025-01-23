using VideoProcessorX.Application.Common;
using VideoProcessorX.Domain.Entities;
using VideoProcessorX.Domain.Interfaces;

namespace VideoProcessorX.Application.Services
{
    public class RegisterUserService
    {
        private readonly IUserRepository _userRepository;

        public RegisterUserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result> RegisterAsync(string username, string email, string password)
        {
            // Verifica se o usuário já existe
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null)
            {
                return Result.Failure("Username already exists");
            }

            // Gera o hash da senha
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Cria o objeto User
            var user = new User
            {
                Username = username,
                Email = email,
                Password = hashedPassword
            };

            // Persiste
            await _userRepository.CreateAsync(user);

            // Retorna sucesso
            return Result.Success();
        }
    }
}
