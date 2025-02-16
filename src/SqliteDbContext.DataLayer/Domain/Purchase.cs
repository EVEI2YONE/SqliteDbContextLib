using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SqlDbContextLib.DataLayer.Domain
{
    public class Purchase
    {
        [Key]
        public int PurchaseId { get; set; }
        public int CustomerId { get; set; }
        public int StoreId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public bool UsedBOGODiscount { get; set; }
        public DateTime PurchaseDate { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
