using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Helpers;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IJwtTokenGenerator _jwt;
        private readonly IPasswordHasher<User> _hasher;

        public AuthService(AppDbContext db, IJwtTokenGenerator jwt, IPasswordHasher<User> hasher)
        {
            _db = db;
            _jwt = jwt;
            _hasher = hasher;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var exists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail);
            if (exists)
            {
                throw new ApiException(StatusCodes.Status409Conflict, "An account with this email already exists.");
            }

            if (request.TeamId.HasValue)
            {
                var teamExists = await _db.Teams.AnyAsync(t => t.Id == request.TeamId.Value);
                if (!teamExists)
                {
                    throw new ApiException(StatusCodes.Status400BadRequest, "The specified team does not exist.");
                }
            }

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                // Public self-registration is always plain "User" - role escalation must go through
                // the admin-only PATCH /api/users/{id}/role endpoint, never through registration input.
                Role = UserRole.User,
                TeamId = request.TeamId
            };

            user.PasswordHash = _hasher.HashPassword(user, request.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _db.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            // Same generic message whether the email doesn't exist or the password is wrong -
            // avoids leaking which emails are registered.
            if (user == null)
            {
                throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid email or password.");
            }

            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid email or password.");
            }

            return BuildAuthResponse(user);
        }

        private AuthResponseDto BuildAuthResponse(User user)
        {
            var (token, expiresAt) = _jwt.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    TeamId = user.TeamId,
                    TeamName = user.Team?.Name,
                    CreatedAt = user.CreatedAt
                }
            };
        }
    }
}
