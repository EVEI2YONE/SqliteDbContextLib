using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SqlDbContextLib.DataLayer.Domain
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
