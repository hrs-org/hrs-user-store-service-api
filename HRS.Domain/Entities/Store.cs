using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("Stores")]
public class Store
{
    [Key] public int Id { get; set; }

    [Required][MaxLength(200)] public string Name { get; set; } = null!;

    [MaxLength(1000)] public string? Description { get; set; }

    [MaxLength(500)] public string? Address { get; set; }

    [MaxLength(20)] public string? PhoneNumber { get; set; }
}
