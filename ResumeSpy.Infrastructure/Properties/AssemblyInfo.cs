using System.Runtime.CompilerServices;

// Allow the test project to access internal members (e.g. ResumeImportService.DecodeText)
// so encoding-detection logic can be unit-tested without making it public API.
[assembly: InternalsVisibleTo("ResumeSpy.Tests")]
