using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Data
{
    public enum PaymentStatus
    {
        Paid=0,
        Pending=1,
        Partial=2
    }

    public enum ApproveStatus
    {
         Pending=0,
        Approved=1,
        Rejected=2
    }
}
