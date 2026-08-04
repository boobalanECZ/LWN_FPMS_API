using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
[Table("USER_MASTER")]
public class UserMaster
{
    [Key]
    [Column("User_ID")]
    public int Id { get; set; }
    [Column("User_Code")]
    public string? UserCode { get; set; }
    [Column("User_Name")]
    public string? UserName { get; set; }
    [Column("Employee_ID")]
    public string? EmployeeId { get; set; }
    [Column("Department_ID")]
    public int DepartmentId { get; set; }
    [Column("Password_Hash")]
    public string? PasswordHash { get; set; }
    [Column("Is_Active")]
    public bool IsActive { get; set; }
    [Column("Created_By")]
    public int? CreatedBy { get; set; }
    [Column("Created_On")]
    public DateTime? CreatedOn { get; set; }
    [Column("Modified_By")]
    public int? ModifiedBy { get; set; }
    [Column("Modified_On")]
    public DateTime? ModifiedOn { get; set; }
    [NotMapped]
    public string? DepartmentName { get; set; }
    [Column("IsLoggedIn")]
    public bool IsLoggedIn { get; set; }
    [Column("SessionId")]
    public Guid? SessionId { get; set; }
    [Column("DeviceId")]
    public string? DeviceId { get; set; }
    [Column("LoginTime")]
    public DateTime? LoginTime { get; set; }
    [Column("LastActivity")]
    public DateTime? LastActivity { get; set; }
}
