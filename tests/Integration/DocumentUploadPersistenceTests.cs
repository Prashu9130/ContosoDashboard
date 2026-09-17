using ContosoDashboard.Tests.TestSupport;

namespace ContosoDashboard.Tests.Integration;

public class DocumentUploadPersistenceTests : IClassFixture<DocumentTestFixture>
{
    private readonly DocumentTestFixture fixture;
    public DocumentUploadPersistenceTests(DocumentTestFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task Project_upload_requires_project_membership_and_task_project_match()
    {
        fixture.Scanner.Status = MalwareScanStatus.Clean;
        var service = new DocumentService(fixture.Db, fixture.Storage, fixture.Scanner);
        var rejected = await service.UploadAsync(4, new DocumentMetadata("Unknown project", "Reports", ProjectId: 999), [new UploadFileInput("a.pdf", "application/pdf", 1, _ => Task.FromResult<Stream>(new MemoryStream([1]))) ]);
        Assert.Equal(DocumentRejectionCode.AssociationUnauthorized, rejected.Single().RejectionCode);
        var accepted = await service.UploadAsync(4, new DocumentMetadata("Task", "Reports", ProjectId: 1, TaskId: 1), [new UploadFileInput("a.pdf", "application/pdf", 1, _ => Task.FromResult<Stream>(new MemoryStream([1]))) ]);
        Assert.True(accepted.Single().Succeeded);
        var row = fixture.Db.Documents.Single(d => d.DocumentId == accepted.Single().DocumentId);
        Assert.Equal(row.ProjectId, 1);
    }
}