using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Services.Classes;
using ECommerce.API.Services.Interfaces;
using ECommerce.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ECommerce.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _context = TestDbContext.CreateInMemoryContext();
        _mockJwtService = new Mock<IJwtService>();
        
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:Secret", "test-secret-key"},
            {"JwtSettings:Issuer", "test-issuer"},
            {"JwtSettings:Audience", "test-audience"},
            {"JwtSettings:ExpiryMinutes", "60"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        _authService = new AuthService(_context, _mockJwtService.Object, configuration);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ShouldCreateUser()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-token");

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(registerDto.Email, result.Email);
        Assert.Equal(registerDto.FirstName, result.FirstName);
        Assert.Equal(registerDto.LastName, result.LastName);
        Assert.Equal("test-token", result.Token);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        Assert.NotNull(userInDb);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ShouldThrowException()
    {
        // Arrange
        var existingUser = new User
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "Customer"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "jane.smith@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(registerDto));
        Assert.Equal("User with this email already exists", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var password = "Password123!";
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Customer"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "john.doe@example.com",
            Password = password
        };

        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-token");

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal("test-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ShouldThrowException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginDto));
        Assert.Equal("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ShouldThrowException()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            Role = "Customer"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "john.doe@example.com",
            Password = "WrongPassword123!"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginDto));
        Assert.Equal("Invalid email or password", exception.Message);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
