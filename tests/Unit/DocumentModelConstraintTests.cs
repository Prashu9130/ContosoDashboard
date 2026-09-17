namespace ContosoDashboard.Tests.Unit;

public class DocumentModelConstraintTests
{
    [Fact]
    public void Document_validates_required_category_size_and_association_rules()
    {
        var document = new Document { Title = " ", Category = "Nope", FileSize = 0, TaskId = 1, ProjectId = null, UploadedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(document, new ValidationContext(document), results, true));
        var customResults = document.Validate(new ValidationContext(document)).ToList();
        Assert.Contains(customResults, r => r.ErrorMessage!.Contains("Title", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(customResults, r => r.ErrorMessage!.Contains("Category", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(customResults, r => r.ErrorMessage!.Contains("size", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(customResults, r => r.ErrorMessage!.Contains("task", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Share_requires_exactly_one_recipient()
    {
        var share = new DocumentShare { UserId = 1, TeamName = "Engineering" };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(share, new ValidationContext(share), results, true));
    }

    [Fact]
    public void Document_uses_integer_id_and_utc_defaults()
    {
        var document = new Document();
        Assert.IsType<int>(document.DocumentId);
        Assert.Equal(DateTimeKind.Utc, document.UploadedDate.Kind);
        Assert.Equal(DateTimeKind.Utc, document.UpdatedDate.Kind);
    }
}