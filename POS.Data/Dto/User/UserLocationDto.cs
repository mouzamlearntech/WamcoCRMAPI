﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Data.Dto
{
    public class UserLocationDto
    {
        public Guid UserId { get; set; }
        public Guid LocationId { get; set; }
    }
}
