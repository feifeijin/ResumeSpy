using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Exceptions;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class CiCdSmokeTests
{
    [Fact]
    public void ResumeViewModel_Title_RoundTrips()
    {
        var vm = new ResumeViewModel { Id = "r1", Title = "CI/CD Test Resume" };
        Assert.Equal("CI/CD Test Resume", vm.Title);
    }

    [Fact]
    public void NotFoundException_Message_IsPreserved()
    {
        var ex = new NotFoundException("resource not found");
        Assert.Equal("resource not found", ex.Message);
    }

    [Fact]
    public void UnauthorizedException_Message_IsPreserved()
    {
        var ex = new UnauthorizedException("not authorized");
        Assert.Equal("not authorized", ex.Message);
    }
}
