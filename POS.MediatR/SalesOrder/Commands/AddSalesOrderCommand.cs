﻿using MediatR;
using POS.Data;
using POS.Data.Dto;
using POS.Helper;
using System;
using System.Collections.Generic;

namespace POS.MediatR.CommandAndQuery
{
    public class AddSalesOrderCommand : IRequest<ServiceResponse<SalesOrderDto>>
    {
        public string OrderNumber { get; set; }
        public string Note { get; set; }
        public string TermAndCondition { get; set; }
        public bool IsSalesOrderRequest { get; set; }
        public DateTime SOCreatedDate { get; set; }
        public SalesOrderStatus Status { get; set; }
        public DateTime DeliveryDate { get; set; }
        public SalesDeliveryStatus DeliveryStatus { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public List<SalesOrderItemDto> SalesOrderItems { get; set; }
        public Guid LocationId { get;  set; }
        public bool IsAllowPayment { get; set; }

        public decimal ShippingPrice { get; set; }

        public string PaymentTerms { get; set; }

        public string ApproveStatus { get; set; }

    }
}
