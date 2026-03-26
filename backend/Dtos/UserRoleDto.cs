namespace Backend.Api.Dtos;

public record class UserRoleDto(string UserId, string RoleName);

public record class UserRoleDetailsDto(string Email, string FullName, string Role);
