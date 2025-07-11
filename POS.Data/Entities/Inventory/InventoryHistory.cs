using POS.Data.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace POS.Data
{
    public class InventoryHistory : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public InventorySourceEnum InventorySource { get; set; }
        public decimal Stock { get; set; }
        public decimal PricePerUnit { get; set; }
        public Decimal PreviousTotalStock { get; set; }
        public Product Product { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder PurchaseOrder { get; set; }
        public Guid? SalesOrderId { get; set; }
        [ForeignKey("SalesOrderId")]
        public SalesOrder SalesOrder { get; set; }
        public Guid LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Location  Location { get; set; }
        public Guid? StockTransferId { get; set; }
        [ForeignKey("StockTransferId")]
        public StockTransfer StockTransfer { get; set; }
        public Guid? DamagedStockId { get; set; }
        [ForeignKey("DamagedStockId")]
        public DamagedStock DamagedStock { get; set; }
    }
}
