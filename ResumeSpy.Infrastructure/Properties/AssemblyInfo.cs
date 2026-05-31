using System.Runtime.CompilerServices;

// Allow the test project to access internal members (e.g. ResumeImportService.DecodeText)
// so encoding-detection logic can be unit-tested without making it public API.
[assembly: InternalsVisibleTo("ResumeSpy.Tests")]

// The UI composition root needs the internal TranslatorFactory.HttpClientName
// constant to register the named HttpClient with the resilience handler.
[assembly: InternalsVisibleTo("ResumeSpy.UI")]
