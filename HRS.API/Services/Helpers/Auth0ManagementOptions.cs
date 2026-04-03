namespace HRS.API.Services.Helpers;

public class Auth0ManagementOptions
{
    public string Domain { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public string CustomerRoleId { get; set; } = string.Empty;
    public string OwnerRoleId { get; set; } = string.Empty;
    public string EmployeeRoleId { get; set; } = string.Empty;
    public string ManagerRoleId { get; set; } = string.Empty;
    public string AdminRoleId { get; set; } = string.Empty;
}
