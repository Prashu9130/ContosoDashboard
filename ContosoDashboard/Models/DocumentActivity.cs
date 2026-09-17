using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
    [Key] public int DocumentActivityId { get; set; }
    public int? DocumentId { get; set; }
    public int ActorUserId { get; set; }
    [Required, MaxLength(30)] public string ActivityType { get; set; } = string.Empty;
    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;
    [MaxLength(255)] public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public int? TargetUserId { get; set; }
    [MaxLength(100)] public string? TargetTeamName { get; set; }
    [MaxLength(2000)] public string? Details { get; set; }
    public Document? Document { get; set; }
    public User ActorUser { get; set; } = null!;
}