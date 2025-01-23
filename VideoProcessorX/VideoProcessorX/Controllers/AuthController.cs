using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VideoProcessorX.Domain.Entities;
using VideoProcessorX.Infrastructure.Persistence;
using VideoProcessorX.WebApi.DTOs.Auth;

namespace VideoProcessorX.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Endpoint para registrar usuário (opcional para MVP, você pode inserir direto no BD se quiser)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto model)
        {
            // Verifica se já existe
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return BadRequest("Username already exists");

            // Cria o User
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                // Aqui, armazenamos a senha hasheada (por ex. Bcrypt, PBKDF2, etc.)
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully");
        }

        // Endpoint para Login (gera token JWT)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null)
                return Unauthorized("Invalid username or password");

            // Verifica hash da senha
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                return Unauthorized("Invalid username or password");

            // Gera token JWT
            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // Função auxiliar para gerar token
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // Claims - aqui podemos adicionar mais (e.g. roles)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("username", user.Username)
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
