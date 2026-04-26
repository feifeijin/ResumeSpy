using System.ComponentModel.DataAnnotations;

namespace ResumeSpy.Core.Entities.General
{
    public class PromptTemplate : Base<int>
    {
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string SystemMessage { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int Version { get; set; } = 1;
    }
}
