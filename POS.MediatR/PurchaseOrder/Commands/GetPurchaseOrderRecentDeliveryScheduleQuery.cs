﻿using MediatR;
using POS.Data.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.MediatR
{
    public class GetPurchaseOrderRecentDeliveryScheduleQuery : IRequest<List<PurchaseOrderRecentDeliverySchedule>>
    {
    }
}
