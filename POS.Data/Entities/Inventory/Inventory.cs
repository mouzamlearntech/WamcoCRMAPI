using POS.Data.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace POS.Data
{
    public class Inventory : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public decimal Stock { get; set; }
        public Product Product { get; set; }
        public decimal AveragePurchasePrice { get; set; }
        public decimal AverageSalesPrice { get; set; }
        public Guid LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Location  Location { get; set; }
    }
}
