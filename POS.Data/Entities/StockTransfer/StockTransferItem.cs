using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace POS.Data.Entities
{
    public class StockTransferItem : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid StockTransferId { get; set; }
        [ForeignKey("StockTransferId")]
        public StockTransfer StockTransfer { get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ShippingCharge { get; set; }
        public decimal SubTotal { get; set; }
        public Guid? UnitId { get; set; }
        [ForeignKey("UnitId")]
        public UnitConversation Unit { get; set; }

    }

}
