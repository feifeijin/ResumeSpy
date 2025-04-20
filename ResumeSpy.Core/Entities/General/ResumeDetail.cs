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
        public required string ResumeId { get; set; }
        public string? Name { get; set; }
        public string? Language { get; set; }
        public string? Content { get; set; }
        public bool IsDefault { get; set; }

        [ForeignKey("ResumeId")]      
        public virtual Resume? Resume { get; set; }



    }
}