using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLibTests.Domain
{
    public class Store
    {
        [Key]
        public int StoreId { get; set; }
        public string Name { get; set; }
        public int RegionId { get; set; }

        [ForeignKey("RegionId")]
        public virtual Region Region { get; set; }
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
