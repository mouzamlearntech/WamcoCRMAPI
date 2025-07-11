﻿using System;

namespace POS.Data
{
    public class ProductTax : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Guid TaxId { get; set; }
        public Product Product { get; set; }
        public Tax Tax { get; set; }
    }
}
