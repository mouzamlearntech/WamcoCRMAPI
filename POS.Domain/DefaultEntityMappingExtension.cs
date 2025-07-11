﻿using Microsoft.EntityFrameworkCore;
using POS.Data;
using POS.Data.Entities;
using Action = POS.Data.Action;

namespace POS.Domain
{
    public static class DefaultEntityMappingExtension
    {
        public static void DefalutMappingValue(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Action>()
               .Property(b => b.ModifiedDate)
               .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Page>()
                .Property(b => b.ModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<User>()
                .Property(b => b.ModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Role>()
                .Property(b => b.ModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Country>()
              .Property(b => b.ModifiedDate)
              .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<City>()
              .Property(b => b.ModifiedDate)
              .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Supplier>()
              .Property(b => b.ModifiedDate)
              .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<ContactRequest>()
              .Property(b => b.ModifiedDate)
              .HasDefaultValueSql("GETUTCDATE()");


            modelBuilder.Entity<ProductCategory>()
                .Property(b => b.ModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");



            modelBuilder.Entity<PurchaseOrder>()
                .Property(b => b.ModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<PurchaseOrderPayment>()
                .Property(b => b.ModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Expense>()
               .Property(b => b.ModifiedDate)
               .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<ExpenseCategory>()
               .Property(b => b.ModifiedDate)
               .HasDefaultValueSql("GETUTCDATE()");
        }

        public static void DefalutDeleteValueFilter(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
            .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Role>()
            .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Action>()
              .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Page>()
             .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<EmailTemplate>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<EmailSMTPSetting>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Country>()
             .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<City>()
             .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Supplier>()
             .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<SupplierAddress>()
             .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<ContactRequest>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<ProductCategory>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Customer>()
               .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Reminder>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<PurchaseOrder>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<PurchaseOrderPayment>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<SalesOrderPayment>()
              .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Expense>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<ExpenseCategory>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<InquiryAttachment>()
               .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<SalesOrder>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Tax>()
              .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Brand>()
              .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Inquiry>()
              .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<InquiryStatus>()
             .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<InquirySource>()
               .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<UnitConversation>()
               .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Language>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Location>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<StockTransfer>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<StockTransferItem>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Variant>()
                .HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
