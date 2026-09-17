using ContosoDashboard.Tests.TestSupport;

namespace ContosoDashboard.Tests.Unit;

public class UploadWorkflowTests : IClassFixture<DocumentTestFixture>
{
    private readonly DocumentTestFixture fixture;
    public UploadWorkflowTests(DocumentTestFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task Clean_pdf_is_persisted_and_reports_progress()
    {
        fixture.Scanner.Status = MalwareScanStatus.Clean;
        var service = new DocumentService(fixture.Db, fixture.Storage, fixture.Scanner);
        var progress = new List<UploadProgress>();
        var result = await service.UploadAsync(4, new DocumentMetadata("Plan", "Reports", Tags: "mvp"), [File("plan.pdf", "application/pdf", "pdf")], new Progress<UploadProgress>(progress.Add));
        Assert.True(result.Single().Succeeded);
        Assert.Single(fixture.Db.Documents);
        Assert.NotEmpty(fixture.Storage.Files);
        Assert.NotEmpty(progress);
    }

    [Theory]
    [InlineData(MalwareScanStatus.Infected, DocumentRejectionCode.ScanInfected)]
    [InlineData(MalwareScanStatus.Unavailable, DocumentRejectionCode.ScanUnavailable)]
    [InlineData(MalwareScanStatus.Incomplete, DocumentRejectionCode.ScanIncomplete)]
    public async Task Non_clean_scan_leaves_no_document_or_file(MalwareScanStatus status, DocumentRejectionCode code)
    {
        fixture.Scanner.Status = status;
        var service = new DocumentService(fixture.Db, fixture.Storage, fixture.Scanner);
        var before = fixture.Db.Documents.Count();
        var result = await service.UploadAsync(4, new DocumentMetadata("Plan", "Reports"), [File("bad.pdf", "application/pdf", "bad")]);
        Assert.Equal(code, result.Single().RejectionCode);
        Assert.Equal(before, fixture.Db.Documents.Count());
    }

    [Fact]
    public async Task Unsupported_and_oversized_files_are_rejected_independently()
    {
        fixture.Scanner.Status = MalwareScanStatus.Clean;
        var service = new DocumentService(fixture.Db, fixture.Storage, fixture.Scanner);
        var results = await service.UploadAsync(4, new DocumentMetadata("Plan", "Reports"), [File("bad.exe", "application/octet-stream", "x"), new UploadFileInput("large.pdf", "application/pdf", Document.MaxFileSize + 1, _ => Task.FromResult<Stream>(new MemoryStream([1]))) ]);
        Assert.Equal(DocumentRejectionCode.UnsupportedType, results[0].RejectionCode);
        Assert.Equal(DocumentRejectionCode.FileTooLarge, results[1].RejectionCode);
    }

    [Fact]
    public async Task Storage_failure_does_not_create_a_document_or_file()
    {
        fixture.Scanner.Status = MalwareScanStatus.Clean;
        fixture.Storage.FailWrites = true;
        var service = new DocumentService(fixture.Db, fixture.Storage, fixture.Scanner);
        var result = await service.UploadAsync(4, new DocumentMetadata("Storage failure", "Reports"), [File("fail.pdf", "application/pdf", "fail")]);
        Assert.Equal(DocumentRejectionCode.StorageFailed, result.Single().RejectionCode);
        Assert.DoesNotContain(fixture.Db.Documents, d => d.Title == "Storage failure");
        fixture.Storage.FailWrites = false;
    }

    private static UploadFileInput File(string name, string type, string text) => new(name, type, text.Length, _ => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text))));
}