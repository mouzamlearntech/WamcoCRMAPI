using System;
using MediatR;
using POS.Helper;

namespace POS.MediatR.Inventory.Command
{
    public class GetInventoryCountCommand : IRequest<ServiceResponse<int>>
    {
        public Guid ProductId { get; set; }
        public Guid LocationId { get; set; }
    }
}
