using POS.Common.GenericRepository;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Dto.PurchaseOrder;
using POS.Data.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Repository
{
    public interface ISalesOrderRepository : IGenericRepository<SalesOrder>
    {
        Task<SalesOrderList> GetAllSalesOrders(SalesOrderResource salesOrderResource);
        Task<PurchaseSalesTotalDto> GetAllSalesOrdersTotal(SalesOrderResource salesOrderResource);
        Task<List<SalesOrderItemTaxDto>> GetAllSalesOrdersItemTaxTotal(SalesOrderResource salesOrderResource);
    }
}
