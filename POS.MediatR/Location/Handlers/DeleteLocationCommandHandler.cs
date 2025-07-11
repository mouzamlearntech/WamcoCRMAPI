using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Domain;
using POS.Helper;
using POS.MediatR.Location.Commands;
using POS.MediatR.Product.Handler;
using POS.Repository;

namespace POS.MediatR.Location.Handlers
{
    public class DeleteLocationCommandHandler(
        IProductRepository productRepository,
            IUnitOfWork<POSDbContext> uow,
            IPurchaseOrderRepository purchaseOrderRepository,
            ISalesOrderRepository salesOrderRepository,
            ILogger<DeleteLocationCommandHandler> logger,
            IStockTransferRepository stockTransferRepository,
            ILocationRepository locationRepository) : IRequestHandler<DeleteLocationCommand, ServiceResponse<bool>>
    {

        public async Task<ServiceResponse<bool>> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
        {
            var entityExist = await locationRepository.FindAsync(request.Id);
            if (entityExist == null)
            {
                return ServiceResponse<bool>.Return404();
            }

            var exitingPurchaseOrder = purchaseOrderRepository
              .AllIncluding(c => c.PurchaseOrderItems)
              .Where(c => c.LocationId == request.Id).Any();

            if (exitingPurchaseOrder)
            {
                logger.LogError("Location can not be Deleted because it is use in Purchase Order");
                return ServiceResponse<bool>.Return409("Location can not be Deleted because it is use in Purchase Order");
            }

            var exitingSalesOrder = salesOrderRepository
               .AllIncluding(c => c.SalesOrderItems)
               .Where(c => c.LocationId == request.Id).Any();

            if (exitingSalesOrder)
            {
                logger.LogError("Location can not be Deleted because it is use in Sales Order");
                return ServiceResponse<bool>.Return409("Location can not be Deleted because it is use in Sales Order");
            }

            var stockTransferItem = stockTransferRepository.All
               .Where(c => c.FromLocationId == request.Id || c.ToLocationId == request.Id).Any();

            if (stockTransferItem)
            {
                logger.LogError("Location can not be Deleted because it is use in Sales Order");
                return ServiceResponse<bool>.Return409("Location can not be Deleted because it is use in Stock Transfer");
            }

            locationRepository.Delete(request.Id);
            if (await uow.SaveAsync() <= 0)
            {
                return ServiceResponse<bool>.Return500();
            }
            return ServiceResponse<bool>.ReturnResultWith200(true);
        }
    }
}
