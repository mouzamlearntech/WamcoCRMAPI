﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.MediatR.SalesOrder.Commands;
using POS.Repository;


namespace POS.MediatR.SalesOrder.Handlers
{
    public class GetSalesOrderItemsCommandHandler : IRequestHandler<GetSalesOrderItemsCommand, List<SalesOrderItemDto>>
    {
        private readonly ISalesOrderItemRepository _salesOrderItemRepository;

        public GetSalesOrderItemsCommandHandler(ISalesOrderItemRepository salesOrderItemRepository)
        {
            _salesOrderItemRepository = salesOrderItemRepository;
        }

        public async Task<List<SalesOrderItemDto>> Handle(GetSalesOrderItemsCommand request, CancellationToken cancellationToken)
        {

            var itemsQuery = _salesOrderItemRepository.AllIncluding(c => c.Product.Unit, cs => cs.SalesOrderItemTaxes)
                .Where(c => c.SalesOrderId == request.Id);

            if (request.IsReturn)
            {
                itemsQuery = itemsQuery.Where(c => c.Status == PurchaseSaleItemStatusEnum.Return);
            }

            var items = await itemsQuery
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new SalesOrderItemDto
                {
                    ProductName = c.Product.Name,
                    UnitName = c.UnitConversation.Name,
                    UnitPrice = c.UnitPrice,
                    Quantity = c.Quantity,
                    DiscountPercentage = c.DiscountPercentage,
                    Discount = c.Discount,
                    TaxValue = c.TaxValue,
                    ProductId = c.ProductId,
                    SalesOrderId = c.SalesOrderId,
                    Id = c.Id,
                    Status = c.Status,
                    UnitId = c.UnitId,
                    SalesOrderItemTaxes = c.SalesOrderItemTaxes.Select(c => new SalesOrderItemTaxDto
                    {
                        TaxName = c.Tax.Name,
                        TaxPercentage = c.Tax.Percentage,
                    }).ToList()
                }).ToListAsync();
            return items;
        }
    }
}
