using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class DocumentShare : IValidatableObject
{
    [Key] public int DocumentShareId { get; set; }
    public int DocumentId { get; set; }
    public int? UserId { get; set; }
    [MaxLength(100)] public string? TeamName { get; set; }
    public int SharedByUserId { get; set; }
    public DateTime SharedDate { get; set; } = DateTime.UtcNow;
    public Document Document { get; set; } = null!;
    public User? User { get; set; }
    public User SharedByUser { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((UserId.HasValue) == !string.IsNullOrWhiteSpace(TeamName)) yield return new ValidationResult("Exactly one share recipient is required.", [nameof(UserId), nameof(TeamName)]);
        if (SharedDate.Kind != DateTimeKind.Utc) yield return new ValidationResult("SharedDate must be UTC.", [nameof(SharedDate)]);
    }
}