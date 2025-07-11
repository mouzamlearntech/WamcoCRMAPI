using MediatR;
using POS.Data;
using POS.Repository;

namespace POS.MediatR
{
    public class GetStockAlertCommand : IRequest<StockAlertList>
    {
        public StockAlertResource StockAlertResource { get; set; }
    }
}
