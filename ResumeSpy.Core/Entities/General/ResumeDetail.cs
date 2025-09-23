using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Entities.General
{
    [Table("ResumeDetails")]
    public class ResumeDetail : Base<string>
    {
        [Required]
        public string ResumeId { get; set; }
        public string? Name { get; set; }
        public string? Language { get; set; }
        public string? Content { get; set; }
        public string? ResumeImgPath { get; set; }
        public bool IsDefault { get; set; }

        public virtual Resume? Resume { get; set; }
    }
}