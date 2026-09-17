using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class Document : IValidatableObject
{
    public const long MaxFileSize = 25 * 1024 * 1024;
    public static readonly string[] AllowedCategories = ["Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"];

    [Key] public int DocumentId { get; set; }
    [Required, MaxLength(255)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [Required, MaxLength(100)] public string Category { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Tags { get; set; }
    [Required, MaxLength(255)] public string OriginalFileName { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string StorageKey { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string FileType { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string FileExtension { get; set; } = string.Empty;
    [Range(1, MaxFileSize)] public long FileSize { get; set; }
    public int UploaderId { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public User Uploader { get; set; } = null!;
    public Project? Project { get; set; }
    public TaskItem? Task { get; set; }
    public ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title)) yield return new ValidationResult("Title is required.", [nameof(Title)]);
        if (!AllowedCategories.Contains(Category, StringComparer.Ordinal)) yield return new ValidationResult("Category is not supported.", [nameof(Category)]);
        if (FileSize <= 0 || FileSize > MaxFileSize) yield return new ValidationResult("File size must be between 1 byte and 25 MiB.", [nameof(FileSize)]);
        if (!Path.GetFileName(StorageKey).Equals(StorageKey.Replace('\\', '/').Split('/').Last(), StringComparison.Ordinal)) yield return new ValidationResult("Storage key must be relative.", [nameof(StorageKey)]);
        if (TaskId.HasValue && !ProjectId.HasValue) yield return new ValidationResult("A task document must belong to a project.", [nameof(TaskId), nameof(ProjectId)]);
        if (UploadedDate.Kind != DateTimeKind.Utc) yield return new ValidationResult("UploadedDate must be UTC.", [nameof(UploadedDate)]);
        if (UpdatedDate.Kind != DateTimeKind.Utc) yield return new ValidationResult("UpdatedDate must be UTC.", [nameof(UpdatedDate)]);
    }
}