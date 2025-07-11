using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Data.Entities
{
    public class DailyStock
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        public decimal OpeningStock { get; set; }
        public decimal ClosingStock { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal QuantityPurchased { get; set; }
        public decimal QuantityDamaged { get; set; }
        public decimal QuantitySoldReturned { get; set; }
        public decimal QuantityPurchasedReturned { get; set; }
        public decimal QuantityAdjusted { get; set; }
        public decimal QuantityToTransfter { get; set; }
        public decimal QuantityFromTransfter { get; set; }
        public DateTime DailyStockDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public Guid LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Location Location { get; set; }
    }
}
