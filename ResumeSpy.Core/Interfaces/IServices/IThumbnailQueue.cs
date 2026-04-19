namespace ResumeSpy.Core.Interfaces.IServices
{
    /// <summary>
    /// Represents a deferred thumbnail generation/deletion task.
    /// </summary>
    /// <param name="ResumeDetailId">ID of the resume detail whose thumbnail needs updating.</param>
    /// <param name="ResumeId">Parent resume ID (used to build the storage key).</param>
    /// <param name="Content">New markdown content. Empty string means delete only.</param>
    /// <param name="OldImagePath">Public URL of the existing thumbnail to delete, if any.</param>
    public record ThumbnailTask(
        string ResumeDetailId,
        string ResumeId,
        string Content,
        string? OldImagePath);

    /// <summary>
    /// A non-blocking queue for background thumbnail generation.
    /// Implementations process tasks asynchronously so that save operations
    /// return immediately without waiting for image rendering and Supabase uploads.
    /// </summary>
    public interface IThumbnailQueue
    {
        /// <summary>
        /// Enqueues a thumbnail task. Returns immediately; processing happens in the background.
        /// </summary>
        void Enqueue(ThumbnailTask task);
    }
}
