using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Helper;
using POS.MediatR.Inventory.Command;
using POS.Repository;

namespace POS.MediatR.Inventory.Handler
{
    public class GetInventoryCountCommandHandler : IRequestHandler<GetInventoryCountCommand, ServiceResponse<int>>
    {
        private readonly IInventoryRepository _inventoryRepository;

        public GetInventoryCountCommandHandler(IInventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<ServiceResponse<int>> Handle(GetInventoryCountCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty && request.LocationId == Guid.Empty)
            {
                return ServiceResponse<int>.Return404("ProductId and LocationId cannot be empty");
            }
            var inventory = await _inventoryRepository.All
                .Where(c => c.ProductId == request.ProductId && c.LocationId == request.LocationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (inventory == null)
            {
                return ServiceResponse<int>.ReturnResultWith200(0);
            }

            return ServiceResponse<int>.ReturnResultWith200((int)inventory.Stock);
        }
    }
}
