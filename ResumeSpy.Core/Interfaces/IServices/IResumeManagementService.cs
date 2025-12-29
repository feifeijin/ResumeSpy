using ResumeSpy.Core.Entities.Business;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IResumeManagementService
    {
        /// <summary>
        /// Creates a ResumeDetail. If ResumeId is provided, creates only the detail.
        /// If ResumeId is null/empty, creates a new Resume first, then the detail.
        /// </summary>
        /// <param name="model">The ResumeDetail to create</param>
        /// <param name="userId">Optional user ID for authenticated users</param>
        /// <param name="guestSessionId">Optional guest session ID for guest users</param>
        /// <param name="ipAddress">IP address of the creator</param>
        /// <returns>The created ResumeDetail with proper ResumeId assigned</returns>
        Task<ResumeDetailViewModel> CreateResumeDetailAsync(ResumeDetailViewModel model, string? userId = null, Guid? guestSessionId = null, string? ipAddress = null);

        /// <summary>
        /// Clones a Resume and all its associated ResumeDetails in a single transaction.
        /// </summary>
        /// <param name="resumeId">The ID of the Resume to clone</param>
        /// <returns>The cloned Resume with new ID</returns>
        Task<ResumeViewModel> CloneResumeAsync(string resumeId);

        /// <summary>
        /// Sets a ResumeDetail as the default and updates the Resume's image path accordingly.
        /// </summary>
        /// <param name="resumeDetailId">The ID of the ResumeDetail to set as default</param>
        /// <returns>Task representing the operation</returns>
        Task SetDefaultResumeDetailAsync(string resumeDetailId);

        /// <summary>
        /// Updates the content of a ResumeDetail and, if it's the default, also updates the parent Resume's image path.
        /// </summary>
        /// <param name="model">The ResumeDetailViewModel with updated content.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateResumeDetailModelContentAsync(ResumeDetailViewModel model);

        /// <summary>
        /// Converts a guest session to a registered user by reassigning all guest resumes and marking the session as converted.
        /// This operation is performed in a single transaction to ensure atomicity.
        /// </summary>
        /// <param name="guestSessionId">The guest session ID to convert</param>
        /// <param name="userId">The user ID to assign the resumes to</param>
        /// <returns>The number of resumes reassigned</returns>
        Task<int> ConvertGuestToUserAsync(Guid guestSessionId, string userId);
    }
}
