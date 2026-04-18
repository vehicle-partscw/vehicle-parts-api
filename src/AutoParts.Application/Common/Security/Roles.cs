namespace AutoParts.Application.Common.Security;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public static readonly string[] All = { Admin, Staff, Customer };
    public static readonly string[] AdminAndStaff = { Admin, Staff };
}
