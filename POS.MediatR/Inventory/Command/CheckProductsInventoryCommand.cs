﻿using MediatR;
using POS.Data.Dto;
using POS.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.MediatR.Inventory
{
    public class CheckProductsInventoryCommand : IRequest<ServiceResponse<List<ProductUnitQuantityDto>>>
    {
        public Guid LocationId { get; set; }
        public List<ProductUnitQuantityDto> ProductIds { get; set; }
    }
}
