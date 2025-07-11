using System.Threading;
using System.Threading.Tasks;
using MediatR;
using POS.Repository;

namespace POS.MediatR
{
    public class GetAllDailyStockQueryHandler (IDailyStockRepository _dailyStockRepository): IRequestHandler<GetAllDailyStockQuery, DailyStockList>
    {
        public async Task<DailyStockList> Handle(GetAllDailyStockQuery request, CancellationToken cancellationToken)
        {
            return await _dailyStockRepository.GetAllDailyStocks(request.DailyStockResource);
        }
    }
}
