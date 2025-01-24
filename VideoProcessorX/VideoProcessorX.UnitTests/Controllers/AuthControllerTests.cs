using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using VideoProcessorX.Domain.Entities;
using VideoProcessorX.Infrastructure.Persistence;
using VideoProcessorX.WebApi.Controllers;
using VideoProcessorX.WebApi.DTOs.Auth;

namespace VideoProcessorX.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly AuthController _controller;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthControllerTests()
        {
            // Configurar banco em memória
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _dbContext = new AppDbContext(options);

            // Configurar valores de JWT em IConfiguration
            var inMemorySettings = new Dictionary<string, string>
            {
                {"Jwt:Key", "SuperSecretKeyForJwt"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Instanciar o controller
            _controller = new AuthController(_dbContext, _configuration);
        }

        [Fact]
        public async Task Register_ShouldReturnOk_WhenUserIsSuccessfullyRegistered()
        {
            // Arrange
            var dto = new UserRegisterDto
            {
                Username = "new_user",
                Email = "new_user@test.com",
                Password = "password123"
            };

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User registered successfully", okResult.Value);
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenUserAlreadyExists()
        {
            // Arrange
            var existingUser = new User
            {
                Username = "existing_user",
                Email = "existing_user@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("password123")
            };

            // Adiciona usuário existente ao banco
            _dbContext.Users.Add(existingUser);
            await _dbContext.SaveChangesAsync();

            var dto = new UserRegisterDto
            {
                Username = "existing_user",
                Email = "existing_user@test.com",
                Password = "password123"
            };

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Username already exists", badRequestResult.Value);
        }
    }
}
