using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IServices
{
    /// <summary>
    /// Defines a service for generating images, such as thumbnails from text.
    /// </summary>
    public interface IImageGenerationService
    {
        /// <summary>
        /// Generates an image from a given text, saves it, and returns the relative path.
        /// </summary>
        /// <param name="text">The text to generate the image from.</param>
        /// <param name="uniqueIdentifier">A unique identifier to use for the filename to prevent collisions.</param>
        /// <returns>The relative path to the saved image (e.g., /images/resumes/my-image.png).</returns>
        Task<string> GenerateThumbnailAsync(string text, string uniqueIdentifier);
    }
}
