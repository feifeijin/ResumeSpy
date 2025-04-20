using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Entities.General
{
    [Table("Resumes")]
    public class Resume : Base<string>
    {

        [Required]
        public string? Title { get; set; }
        
        public int ResumeDetailCount { get; set; }
        
        public string? ResumeImgPath { get; set; }
       
        public virtual ICollection<ResumeDetail>? ResumeDetails { get; set; }
    }
}
