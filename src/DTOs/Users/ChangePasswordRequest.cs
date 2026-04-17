namespace back_api_splitwise.src.DTOs.Users;

public record ChangePasswordRequest(string OldPassword, string NewPassword, string ConfirmPassword);
