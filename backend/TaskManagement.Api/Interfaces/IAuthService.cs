using TaskManagement.Api.DTOs;

namespace TaskManagement.Api.Interfaces
{
    public class ApiException : Exception
    {
        public int StatusCode { get; }
        public ApiException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    }

    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    }
}
