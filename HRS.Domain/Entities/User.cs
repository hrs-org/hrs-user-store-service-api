using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRS.Shared.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRS.Domain.Entities;

[Table("Users")]
[Index(nameof(Auth0UserId), IsUnique = true)]
public class User
{
    [Key] public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Auth0UserId { get; set; } = null!;

    [Required][MaxLength(100)] public string FirstName { get; set; } = null!;

    [Required][MaxLength(100)] public string LastName { get; set; } = null!;

    [Required][MaxLength(150)] public string Email { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Customer;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public int? StoreId { get; set; }

    [ForeignKey(nameof(CreatedBy))] public virtual User? CreatedByUser { get; set; }

    [ForeignKey(nameof(UpdatedBy))] public virtual User? UpdatedByUser { get; set; }
    [ForeignKey(nameof(StoreId))] public virtual Store? Store { get; set; }
}
