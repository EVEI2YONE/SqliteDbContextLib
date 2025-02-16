using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace SqliteDbContextLibTests.Domain
{
    public class Region
    {
        [Key]
        public int RegionId { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
    }
}
