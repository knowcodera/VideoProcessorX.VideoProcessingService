using Moq;
using VideoProcessorX.Application.Services;
using VideoProcessorX.Domain.Entities;
using VideoProcessorX.Domain.Interfaces;

namespace VideoProcessorX.UnitTests.Services
{
    public class RegisterUserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly RegisterUserService _registerUserService;

        public RegisterUserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _registerUserService = new RegisterUserService(_mockUserRepository.Object);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnSuccess_WhenUserIsNew()
        {
            // Arrange
            var username = "new_user";
            var email = "new_user@test.com";
            var password = "password123";

            _mockUserRepository.Setup(repo => repo.GetByUsernameAsync(username))
                .ReturnsAsync((User)null);

            // Act
            var result = await _registerUserService.RegisterAsync(username, email, password);

            // Assert
            Assert.True(result.Succeeded);
            _mockUserRepository.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnFailure_WhenUsernameExists()
        {
            // Arrange
            var username = "existing_user";
            var email = "existing_user@test.com";
            var password = "password123";

            _mockUserRepository.Setup(repo => repo.GetByUsernameAsync(username))
                .ReturnsAsync(new User { Username = username });

            // Act
            var result = await _registerUserService.RegisterAsync(username, email, password);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal("Username already exists", result.Error);
            _mockUserRepository.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
