namespace Backend.Dtos
{
    public record UserRegisterDto(string Username, string Email, string Password);
    public record UserLoginDto(string Email, string Password);
    public record LoginResponseDto(string AccessToken, DateTime ExpiresAt, string Username, string Role);
    public record UserInfoDto(int Id, string Username, string Email, string Role);
}
