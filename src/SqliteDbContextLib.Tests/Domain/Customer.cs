using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLibTests.Domain
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}
