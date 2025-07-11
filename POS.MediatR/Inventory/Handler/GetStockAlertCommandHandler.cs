using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using POS.Repository;

namespace POS.MediatR
{
    public class GetStockAlertCommandHandler(
        IInventoryRepository inventoryRepository)
        : IRequestHandler<GetStockAlertCommand, StockAlertList>
    {
        public async Task<StockAlertList> Handle(GetStockAlertCommand request, CancellationToken cancellationToken)
        {
            return await inventoryRepository.GetStockAlertsAsync(request.StockAlertResource);
        }
    }
}
