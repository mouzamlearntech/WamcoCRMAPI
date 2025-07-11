using System;

namespace POS.Data.Dto
{
    public class DailyStockDto
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; }
        public ProductDto Product { get; set; }
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
        public string LocationId { get; set; }
        public LocationDto Location { get; set; }
    }
}
