using MediatR;
using POS.Data.Resources;
using POS.Repository;

namespace POS.MediatR
{
    public class GetAllDailyStockQuery : IRequest<DailyStockList>
    {
        public DailyStockResource DailyStockResource { get; set; }
    }

}
