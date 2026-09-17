using Microsoft.AspNetCore.Mvc.Testing;

namespace ContosoDashboard.Tests.TestSupport;

public sealed class LocalHostFixture : WebApplicationFactory<Program>
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}