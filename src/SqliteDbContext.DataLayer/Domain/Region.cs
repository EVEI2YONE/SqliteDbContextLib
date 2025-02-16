using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SqlDbContextLib.DataLayer.Domain
{
    public class Region
    {
        [Key]
        public int RegionId { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
    }
}
