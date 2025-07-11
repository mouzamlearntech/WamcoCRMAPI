using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Data.Dto
{
    public class ProductInventoryStockDto
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public double? Stock { get; set; }
        public string UnitName { get; set; }
        public List<Inventory> Inventories { get; set; }
        public Guid UnitId { get; set; }
        public Guid? ParentUnitId { get; set; }
    }
}
