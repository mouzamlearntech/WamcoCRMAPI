using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Domain;
using POS.Helper;
using POS.MediatR.CommandAndQuery;
using POS.Repository;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.MediatR.Handlers
{
    public class AddSalesOrderCommandHandler : IRequestHandler<AddSalesOrderCommand, ServiceResponse<SalesOrderDto>>
    {
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IUnitOfWork<POSDbContext> _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<AddSalesOrderCommandHandler> _logger;
        private readonly IInventoryRepository _inventoryRepository;

        public AddSalesOrderCommandHandler(
            ISalesOrderRepository salesOrderRepository,
            IUnitOfWork<POSDbContext> uow,
            IMapper mapper,
            ILogger<AddSalesOrderCommandHandler> logger,
            IInventoryRepository inventoryRepository)
        {
            _salesOrderRepository = salesOrderRepository;
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _inventoryRepository = inventoryRepository;
        }

        public async Task<ServiceResponse<SalesOrderDto>> Handle(AddSalesOrderCommand request, CancellationToken cancellationToken)
        {

            var existingSONumber = _salesOrderRepository.All.Any(c => c.OrderNumber == request.OrderNumber);
            if (existingSONumber)
            {
                return ServiceResponse<SalesOrderDto>.Return409("Sales Order Number is already Exists.");
            }

            var salesOrder = _mapper.Map<POS.Data.SalesOrder>(request);
            salesOrder.PaymentStatus = PaymentStatus.Pending;
            salesOrder.ApproveStatus = (int)ApproveStatus.Pending;
            salesOrder.SalesOrderItems.ForEach(item =>
            {
                item.Product = null;
                item.SalesOrderItemTaxes.ForEach(tax => { tax.Tax = null; });
                item.CreatedDate = DateTime.UtcNow;
            });
            _salesOrderRepository.Add(salesOrder);

            if (!request.IsSalesOrderRequest && request.DeliveryStatus == SalesDeliveryStatus.DELIVERED)
            {

                var inventories = request.SalesOrderItems
                    .Select(cs => new InventoryDto
                    {
                        ProductId = cs.ProductId,
                        LocationId = request.LocationId,
                        PricePerUnit = cs.UnitPrice,
                        SalesOrderId = salesOrder.Id,
                        Stock = cs.Quantity,
                        UnitId = cs.UnitId,
                        TaxValue = cs.TaxValue,
                        Discount = cs.Discount
                    }).ToList();

                inventories.ForEach(invetory =>
                {
                    invetory = _inventoryRepository.ConvertStockAndPriceToBaseUnit(invetory);
                });

                var inventoriesToAdd = inventories
                    .GroupBy(c => new { c.ProductId, c.LocationId })
                    .Select(cs => new InventoryDto
                    {
                        InventorySource = InventorySourceEnum.SalesOrder,
                        ProductId = cs.Key.ProductId,
                        LocationId = cs.Key.LocationId,
                        PricePerUnit = cs.Sum(d => d.PricePerUnit * d.Stock + d.TaxValue - d.Discount) / cs.Sum(d => d.Stock),
                        SalesOrderId = salesOrder.Id,
                        Stock = cs.Sum(d => d.Stock),
                    }).ToList();

                foreach (var inventory in inventoriesToAdd)
                {
                    await _inventoryRepository.AddInventory(inventory);
                }
            }

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Error while creating Sales Order.");
                return ServiceResponse<SalesOrderDto>.Return500();
            }
            var dto = _mapper.Map<SalesOrderDto>(salesOrder);
            return ServiceResponse<SalesOrderDto>.ReturnResultWith201(dto);
        }
    }
}
