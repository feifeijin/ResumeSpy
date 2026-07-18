using System.ComponentModel.DataAnnotations;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Entities.Business.Auth;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.UI.Models;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class ModelTests
{
    [Fact]
    public void ResumeEntity_FailsValidation_WhenTitleIsEmpty()
    {
        // Purpose: verify required data annotation on Resume.Title is enforced.
        var model = new Resume { Id = "r1", Title = string.Empty };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Resume.Title)));
    }

    [Fact]
    public void ResumeDetailEntity_FailsValidation_WhenResumeIdIsEmpty()
    {
        // Purpose: verify required data annotation on ResumeDetail.ResumeId is enforced.
        var model = new ResumeDetail { Id = "d1", ResumeId = string.Empty };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ResumeDetail.ResumeId)));
    }

    [Fact]
    public void ApiModels_DefaultStringFields_AreDeterministic()
    {
        // Purpose: ensure API models initialize string fields to empty for null-safe serialization.
        var resume = new ResumeModel();
        var detail = new ResumeDetailModel();

        Assert.Equal(string.Empty, resume.Id);
        Assert.Equal(string.Empty, resume.Title);
        Assert.Equal(string.Empty, detail.Id);
        Assert.Equal(string.Empty, detail.Content);
    }

    [Fact]
    public void AuthSyncResponse_DefaultsErrorsToEmptyCollection()
    {
        // Purpose: verify response defaults are deterministic and null-safe for clients.
        var response = new AuthSyncResponse();

        Assert.NotNull(response.Errors);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public void ResumeViewModel_DefaultsGuestFlagToFalse()
    {
        // Purpose: verify default guest flag behavior is false for new instances.
        var vm = new ResumeViewModel { Id = "r1" };
        Assert.False(vm.IsGuest);
    }
}
