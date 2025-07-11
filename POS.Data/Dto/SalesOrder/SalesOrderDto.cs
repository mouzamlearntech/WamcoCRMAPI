using System;
using System.Collections.Generic;



namespace POS.Data.Dto
{
    public class SalesOrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public string Note { get; set; }
        public string TermAndCondition { get; set; }
        public bool IsSalesOrderRequest { get; set; }
        public DateTime SOCreatedDate { get; set; }
        public SalesOrderStatus Status { get; set; }
        public DateTime DeliveryDate { get; set; }
        public SalesDeliveryStatus DeliveryStatus { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public List<SalesOrderItemDto> SalesOrderItems { get; set; }
        public List<SalesOrderPaymentDto> SalesOrderPayments { get; set; }
        public CustomerDto Customer { get; set; }
        public Guid LocationId { get; set; }
        public LocationDto Location { get; set; }
        public decimal ShippingPrice { get; set; }


        public string PaymentTerms {  get; set; }

        public ApproveStatus ApproveStatus { get; set; }

        public DateTime? ModifiedDate { get; set; }
        public string CreatedByName { get; set; }
        public string BusinessLocation { get; set; }
    }
}
