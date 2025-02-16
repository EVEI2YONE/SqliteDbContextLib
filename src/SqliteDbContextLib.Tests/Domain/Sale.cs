using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLibTests.Domain
{
    public class Sale
    {
        [Key]
        public int SaleId { get; set; }
        public int StoreId { get; set; }
        public decimal DiscountBudgetUsed { get; set; }
        public DateTime SaleDate { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }
    }
}
