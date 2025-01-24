using Moq;
using VideoProcessorX.Application.Services;
using VideoProcessorX.Domain.Entities;
using VideoProcessorX.Domain.Interfaces;

namespace VideoProcessorX.UnitTests.Services
{
    public class LoginUserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly LoginUserService _loginUserService;

        public LoginUserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _loginUserService = new LoginUserService(_mockUserRepository.Object);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var username = "valid_user";
            var password = "password123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            _mockUserRepository.Setup(repo => repo.GetByUsernameAsync(username))
                .ReturnsAsync(new User { Username = username, Password = hashedPassword });

            // Act
            var result = await _loginUserService.LoginAsync(username, password);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            Assert.Equal(username, result.Value.Username);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnFailure_WhenUserNotFound()
        {
            // Arrange
            var username = "nonexistent_user";
            var password = "password123";

            _mockUserRepository.Setup(repo => repo.GetByUsernameAsync(username))
                .ReturnsAsync((User)null);

            // Act
            var result = await _loginUserService.LoginAsync(username, password);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal("Invalid username or password", result.Error);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnFailure_WhenPasswordIsInvalid()
        {
            // Arrange
            var username = "valid_user";
            var password = "wrong_password";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");

            _mockUserRepository.Setup(repo => repo.GetByUsernameAsync(username))
                .ReturnsAsync(new User { Username = username, Password = hashedPassword });

            // Act
            var result = await _loginUserService.LoginAsync(username, password);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal("Invalid username or password", result.Error);
        }
    }
}
