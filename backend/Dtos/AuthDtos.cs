namespace Backend.Api.Dtos;

public record GoogleLoginRequestDto(string IdToken);

public record AuthStatusResponseDto(string Message, bool IsLogged);

public record IsAdminResponseDto(bool IsAdmin, List<string> Roles);
