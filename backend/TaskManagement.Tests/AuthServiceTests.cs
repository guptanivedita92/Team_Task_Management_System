using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Helpers;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;
using TaskManagement.Tests.TestHelpers;

namespace TaskManagement.Tests
{
    public class AuthServiceTests
    {
        private static AuthService CreateService(out AppDbContext db)
        {
            db = TestDbContextFactory.Create();
            var jwtOptions = Options.Create(new JwtSettings
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Key = "this-is-a-32-plus-character-test-signing-key",
                ExpiryMinutes = 60
            });
            var jwt = new JwtTokenGenerator(jwtOptions);
            var hasher = new PasswordHasher<User>();
            return new AuthService(db, jwt, hasher);
        }

        [Fact]
        public async Task Register_CreatesUser_WithHashedPassword_NotPlaintext()
        {
            var service = CreateService(out var db);

            var result = await service.RegisterAsync(new RegisterRequestDto
            {
                FullName = "Test User",
                Email = "new.user@example.com",
                Password = "SuperSecret123"
            });

            Assert.NotNull(result.Token);
            Assert.Equal(UserRole.User, result.User.Role); // self-registration always forced to User

            var stored = db.Users.Single();
            Assert.NotEqual("SuperSecret123", stored.PasswordHash);
            Assert.True(stored.PasswordHash.Length > 20);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Throws409()
        {
            var service = CreateService(out _);
            var dto = new RegisterRequestDto { FullName = "A", Email = "dup@example.com", Password = "Password123" };

            await service.RegisterAsync(dto);
            var ex = await Assert.ThrowsAsync<ApiException>(() => service.RegisterAsync(dto));

            Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
        }

        [Fact]
        public async Task Register_IgnoresClientSuppliedAdminRole()
        {
            var service = CreateService(out var db);

            await service.RegisterAsync(new RegisterRequestDto
            {
                FullName = "Sneaky",
                Email = "sneaky@example.com",
                Password = "Password123",
                Role = UserRole.Admin // attempted privilege escalation via registration payload
            });

            var stored = db.Users.Single();
            Assert.Equal(UserRole.User, stored.Role);
        }

        [Fact]
        public async Task Login_CorrectCredentials_ReturnsToken()
        {
            var service = CreateService(out _);
            await service.RegisterAsync(new RegisterRequestDto
            {
                FullName = "Login Test",
                Email = "login@example.com",
                Password = "CorrectPassword1"
            });

            var result = await service.LoginAsync(new LoginRequestDto
            {
                Email = "login@example.com",
                Password = "CorrectPassword1"
            });

            Assert.False(string.IsNullOrEmpty(result.Token));
        }

        [Fact]
        public async Task Login_WrongPassword_Throws401()
        {
            var service = CreateService(out _);
            await service.RegisterAsync(new RegisterRequestDto
            {
                FullName = "Login Test",
                Email = "login2@example.com",
                Password = "CorrectPassword1"
            });

            var ex = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(
                new LoginRequestDto { Email = "login2@example.com", Password = "WrongPassword" }));

            Assert.Equal(StatusCodes.Status401Unauthorized, ex.StatusCode);
        }

        [Fact]
        public async Task Login_UnknownEmail_Throws401_SameMessageAsWrongPassword()
        {
            var service = CreateService(out _);

            var ex = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(
                new LoginRequestDto { Email = "nobody@example.com", Password = "whatever123" }));

            Assert.Equal(StatusCodes.Status401Unauthorized, ex.StatusCode);
            Assert.Equal("Invalid email or password.", ex.Message);
        }
    }
}
