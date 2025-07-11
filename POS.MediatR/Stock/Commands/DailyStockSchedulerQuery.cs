using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using POS.Helper;

namespace POS.MediatR.Stock.Commands
{
    public class DailyStockSchedulerQuery : IRequest<bool>
    {
    }
}
