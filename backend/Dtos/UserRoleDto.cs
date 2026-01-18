namespace Backend.Api.Dtos;

public record class UserRoleDto(int UserId, string RoleName);

public record class UserRoleDetailsDto(string Email, string FullName, string Role);
