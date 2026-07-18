using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IServices
{
    /// <summary>
    /// Defines a service for generating images, such as thumbnails from text.
    /// </summary>
    public interface IImageGenerationService
    {
        /// <summary>
        /// Generates an image from a given text, stores it, and returns the public URL.
        /// </summary>
        /// <param name="text">The text to generate the image from.</param>
        /// <param name="uniqueIdentifier">A unique identifier to use for the filename to prevent collisions.</param>
        /// <returns>The public URL to the stored image.</returns>
        Task<string> GenerateThumbnailAsync(string text, string uniqueIdentifier);

        /// <summary>
        /// Deletes a thumbnail image from storage.
        /// </summary>
        /// <param name="imagePath">The public URL of the image to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteThumbnailAsync(string? imagePath);
    }
}
