using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Core.Entities.General
{
    //Base class for entities common properties
    public class Base<T>
    {
        [Key]
        public T Id { get; set; } // Remove 'required'

        [Column(TypeName = "timestamp")]
        public DateTime? EntryDate { get; set; }

        [Column(TypeName = "timestamp")]
        public DateTime? UpdateDate { get; set; }
    }
}
