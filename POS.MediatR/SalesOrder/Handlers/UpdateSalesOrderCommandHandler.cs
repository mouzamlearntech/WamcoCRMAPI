using AutoMapper;
using Azure.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.Domain;
using POS.Helper;
using POS.MediatR.SalesOrder.Commands;
using POS.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.MediatR.Handlers
{
    public class UpdateSalesOrderCommandHandler
        : IRequestHandler<UpdateSalesOrderCommand, ServiceResponse<SalesOrderDto>>
    {
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly ISalesOrderItemRepository _salesOrderItemRepository;
        private readonly IUnitOfWork<POSDbContext> _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateSalesOrderCommandHandler> _logger;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IDailyStockRepository _dailyStockRepository;

        public UpdateSalesOrderCommandHandler(
            ISalesOrderRepository salesOrderRepository,
            ISalesOrderItemRepository salesOrderItemRepository,
            IUnitOfWork<POSDbContext> uow,
            IMapper mapper,
            ILogger<UpdateSalesOrderCommandHandler> logger,
            IInventoryRepository inventoryRepository,
            IDailyStockRepository dailyStockRepository)
        {
            _salesOrderRepository = salesOrderRepository;
            _salesOrderItemRepository = salesOrderItemRepository;
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _inventoryRepository = inventoryRepository;
            _dailyStockRepository = dailyStockRepository;
        }

        public async Task<ServiceResponse<SalesOrderDto>> Handle(UpdateSalesOrderCommand request, CancellationToken cancellationToken)
        {
            var existingSONumber = _salesOrderRepository.All.Any(c => c.OrderNumber == request.OrderNumber && c.Id != request.Id);
            if (existingSONumber)
            {
                return ServiceResponse<SalesOrderDto>.Return409("Sales Order Number is already Exists.");
            }

            var salesOrderExit = await _salesOrderRepository.FindAsync(request.Id);
            if (salesOrderExit.Status == SalesOrderStatus.Return)
            {
                return ServiceResponse<SalesOrderDto>.Return409("Sales Order can't edit becuase it's already Return.");
            }
            if(!salesOrderExit.IsSalesOrderRequest && salesOrderExit.DeliveryStatus == SalesDeliveryStatus.DELIVERED)
            {
                return ServiceResponse<SalesOrderDto>.Return409("Sales Order can't edit becuase it's already received.");
            }

            var salesOrderItemsExist = await _salesOrderItemRepository.FindBy(c => c.SalesOrderId == request.Id).ToListAsync();
            if (salesOrderItemsExist.Count > 0)
            {
                foreach (var salesOrderItem in salesOrderItemsExist)
                {
                    var inventory = new InventoryDto
                    {
                        ProductId = salesOrderItem.ProductId,
                        LocationId = salesOrderExit.LocationId,
                        Stock = salesOrderItem.Quantity,
                        UnitId = salesOrderItem.UnitId,
                        PricePerUnit = salesOrderItem.UnitPrice,
                        TaxValue = salesOrderItem.TaxValue,
                        Discount = salesOrderItem.Discount,
                        InventorySource = InventorySourceEnum.SalesOrder
                    };
                    inventory = _inventoryRepository.ConvertStockAndPriceToBaseUnit(inventory);
                    inventory.Stock = -1 * inventory.Stock;
                    await _dailyStockRepository.AddDailyStock(inventory);

                }
                if (await _uow.SaveAsync() <= 0)
                {
                    _logger.LogError("Error while creating Purchase Order.");
                    return ServiceResponse<SalesOrderDto>.Return500();
                }
            }


            _salesOrderItemRepository.RemoveRange(salesOrderItemsExist);
            var salesOrderUpdate = _mapper.Map<POS.Data.SalesOrder>(request);
            salesOrderUpdate.SalesOrderItems.ForEach(item =>
            {
                item.Product = null;
                item.SalesOrderItemTaxes.ForEach(tax => { tax.Tax = null; });
            });

            salesOrderExit.OrderNumber = salesOrderUpdate.OrderNumber;
            salesOrderExit.CustomerId = salesOrderUpdate.CustomerId;
            salesOrderExit.Note = salesOrderUpdate.Note;
            salesOrderExit.TermAndCondition = salesOrderUpdate.TermAndCondition;
            salesOrderExit.IsSalesOrderRequest = salesOrderUpdate.IsSalesOrderRequest;
            salesOrderExit.SOCreatedDate = salesOrderUpdate.SOCreatedDate;
            salesOrderExit.Status = salesOrderUpdate.Status;
            salesOrderExit.DeliveryDate = salesOrderUpdate.DeliveryDate;
            salesOrderExit.DeliveryStatus = salesOrderUpdate.DeliveryStatus;
            salesOrderExit.CustomerId = salesOrderUpdate.CustomerId;
            salesOrderExit.TotalAmount = salesOrderUpdate.TotalAmount;
            salesOrderExit.TotalTax = salesOrderUpdate.TotalTax;
            salesOrderExit.TotalDiscount = salesOrderUpdate.TotalDiscount;
            salesOrderExit.SalesOrderItems = salesOrderUpdate.SalesOrderItems;

            salesOrderExit.ShippingPrice = salesOrderUpdate.ShippingPrice;
            salesOrderExit.PaymentTerms = salesOrderUpdate.PaymentTerms;
            salesOrderExit.ApproveStatus = salesOrderUpdate.ApproveStatus;
            salesOrderExit.SalesOrderItems.ForEach(c =>
            {
                c.SalesOrderId = salesOrderUpdate.Id;
                c.SalesOrderItemTaxes.ForEach(tax => { tax.Tax = null; });
                c.CreatedDate = DateTime.UtcNow;
            });

            _salesOrderRepository.Update(salesOrderExit);

            if (!salesOrderExit.IsSalesOrderRequest && salesOrderExit.DeliveryStatus == SalesDeliveryStatus.DELIVERED)
            {

                var inventories = request.SalesOrderItems
                    .Select(cs => new InventoryDto
                    {
                        ProductId = cs.ProductId,
                        LocationId = salesOrderExit.LocationId,
                        PricePerUnit = cs.UnitPrice,
                        SalesOrderId = salesOrderExit.Id,
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
                        SalesOrderId = salesOrderExit.Id,
                        Stock = cs.Sum(d => d.Stock),
                    }).ToList();

                foreach (var inventory in inventoriesToAdd)
                {
                    await _inventoryRepository.AddInventory(inventory);
                }
            }

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Error while Updating Sales Order.");
                return ServiceResponse<SalesOrderDto>.Return500();
            }
            var dto = _mapper.Map<SalesOrderDto>(salesOrderExit);
            return ServiceResponse<SalesOrderDto>.ReturnResultWith201(dto);
        }

        private async Task AddRemoveInvestories(List<SalesOrderItem> salesOrderItems, Guid LocationId, bool isAdded)
        {
            var salesOrderId = salesOrderItems.FirstOrDefault().SalesOrderId;
            var inventories = salesOrderItems
                   .Select(cs => new InventoryDto
                   {
                       ProductId = cs.ProductId,
                       LocationId = LocationId,
                       PricePerUnit = cs.UnitPrice,
                       SalesOrderId = cs.Id,
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
                    SalesOrderId = salesOrderId,
                    Stock = -1 * cs.Sum(d => d.Stock),
                }).ToList();

            foreach (var inventory in inventoriesToAdd)
            {
                await _inventoryRepository.AddInventory(inventory);
            }
        }
    }

    

}
