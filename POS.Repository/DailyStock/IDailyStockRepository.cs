using POS.Common.GenericRepository;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.Data.Resources;
using System.Threading.Tasks;

namespace POS.Repository
{
    public interface IDailyStockRepository : IGenericRepository<DailyStock>
    {
        Task<DailyStockList> GetAllDailyStocks(DailyStockResource dailyStockResource);
        Task AddDailyStock(InventoryDto inventory);
    }
}
