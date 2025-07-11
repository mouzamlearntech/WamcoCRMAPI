using POS.Data;
using POS.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection.Emit;

namespace POS.Domain
{
    public class POSDbContext : IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public POSDbContext(DbContextOptions options) : base(options)
        {
        }
        public override DbSet<User> Users { get; set; }
        public override DbSet<Role> Roles { get; set; }
        public override DbSet<UserClaim> UserClaims { get; set; }
        public override DbSet<UserRole> UserRoles { get; set; }
        public override DbSet<UserLogin> UserLogins { get; set; }
        public override DbSet<RoleClaim> RoleClaims { get; set; }
        public override DbSet<UserToken> UserTokens { get; set; }
        public DbSet<Data.Action> Actions { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<NLog> NLog { get; set; }
        public DbSet<LoginAudit> LoginAudits { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<EmailSMTPSetting> EmailSMTPSettings { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierAddress> SupplierAddresses { get; set; }
        public DbSet<ContactRequest> ContactRequests { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<ReminderNotification> ReminderNotifications { get; set; }
        public DbSet<ReminderUser> ReminderUsers { get; set; }
        public DbSet<ReminderScheduler> ReminderSchedulers { get; set; }
        public DbSet<HalfYearlyReminder> HalfYearlyReminders { get; set; }
        public DbSet<QuarterlyReminder> QuarterlyReminders { get; set; }
        public DbSet<DailyReminder> DailyReminders { get; set; }
        public DbSet<SendEmail> SendEmails { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<PurchaseOrderItemTax> PurchaseOrderItemTaxes { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
        public DbSet<SalesOrderItemTax> SalesOrderItemTaxes { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Tax> Taxes { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductTax> ProductTaxes { get; set; }
        public DbSet<Inquiry> Inquiries { get; set; }
        public DbSet<InquiryActivity> InquiryActivities { get; set; }
        public DbSet<InquiryAttachment> InquiryAttachments { get; set; }
        public DbSet<InquiryNote> InquiryNotes { get; set; }
        public DbSet<InquirySource> InquirySources { get; set; }
        public DbSet<InquiryProduct> InquiryProducts { get; set; }
        public DbSet<InquiryStatus> InquiryStatuses { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<InventoryHistory> InventoryHistories { get; set; }
        public DbSet<PurchaseOrderPayment> PurchaseOrderPayments { get; set; }
        public DbSet<SalesOrderPayment> SalesOrderPayments { get; set; }
        public DbSet<UnitConversation> UnitConversations { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<VariantItem> VariantItems { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<EmailLogAttachment> EmailLogAttachments { get; set; }
        public DbSet<ExpenseTax> ExpenseTaxes { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<PageHelper> Pagehelpers { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }
        public DbSet<ContactAddress> ContactAddresses { get; set; }
        public DbSet<UserLocation> UserLocations { get; set; }
        public DbSet<TableSetting> TableSettings { get; set; }
        public DbSet<DamagedStock> DamagedStocks { get; set; }
        public DbSet<DailyStock> DailyStocks { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(b =>
            {
                // Each User can have many UserClaims
                b.HasMany(e => e.UserClaims)
                    .WithOne(e => e.User)
                    .HasForeignKey(uc => uc.UserId)
                    .IsRequired();

                // Each User can have many UserLogins
                b.HasMany(e => e.UserLogins)
                    .WithOne(e => e.User)
                    .HasForeignKey(ul => ul.UserId)
                    .IsRequired();

                // Each User can have many UserTokens
                b.HasMany(e => e.UserTokens)
                    .WithOne(e => e.User)
                    .HasForeignKey(ut => ut.UserId)
                    .IsRequired();

                // Each User can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.User)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();

                b.HasMany(e => e.UserLocations)
                    .WithOne(e => e.User)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            builder.Entity<DamagedStock>(b =>
            {
                b.HasOne(e => e.ReportedBy)
               .WithMany()
               .HasForeignKey(ur => ur.ReportedId)
              .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(ur => ur.CreatedBy)
               .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Role>(b =>
            {
                // Each Role can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.Role)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                // Each Role can have many associated RoleClaims
                b.HasMany(e => e.RoleClaims)
                    .WithOne(e => e.Role)
                    .HasForeignKey(rc => rc.RoleId)
                    .IsRequired();

                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(e => e.ModifiedByUser)
                    .WithMany()
                    .HasForeignKey(rc => rc.ModifiedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(e => e.DeletedByUser)
                    .WithMany()
                    .HasForeignKey(rc => rc.DeletedBy)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<ReminderUser>(b =>
            {
                b.HasKey(e => new { e.ReminderId, e.UserId });
                b.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<Data.Action>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Page>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<EmailSMTPSetting>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<Customer>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(e => e.BillingAddress)
                  .WithMany()
                  .HasForeignKey(rc => rc.BillingAddressId)
                  .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(e => e.ShippingAddress)
                  .WithMany()
                  .HasForeignKey(rc => rc.ShippingAddressId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SalesOrder>()
                   .HasOne(so => so.Customer)
                   .WithMany()
                   .HasForeignKey(so => so.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            builder.Entity<SalesOrder>()
                .HasOne(so => so.Location)
                .WithMany()
                .HasForeignKey(so => so.LocationId)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            builder.Entity<SalesOrder>()
                .HasOne(so => so.CreatedByUser)
                .WithMany()
                .HasForeignKey(so => so.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict


            builder.Entity<UserLocation>()
                .HasKey(ul => new { ul.UserId, ul.LocationId });


            builder.Entity<VariantItem>()
                .HasKey(vi => vi.Id);

            builder.Entity<VariantItem>()
                .HasOne(vi => vi.CreatedByUser)
                .WithMany()
                .HasForeignKey(vi => vi.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            builder.Entity<VariantItem>()
                .HasOne(vi => vi.Variant)
                .WithMany(v => v.VariantItems)
                .HasForeignKey(vi => vi.VariantId)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            builder.Entity<ExpenseTax>()
        .HasKey(et => et.Id);

            builder.Entity<ExpenseTax>()
                .HasOne(et => et.Expense)
                .WithMany()
                .HasForeignKey(et => et.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            builder.Entity<ExpenseTax>()
                .HasOne(et => et.Tax)
                .WithMany()
                .HasForeignKey(et => et.TaxId)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            builder.Entity<ExpenseTax>()
                .HasOne(et => et.CreatedByUser)
                .WithMany()
                .HasForeignKey(et => et.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            builder.Entity<InquiryProduct>()
                .HasKey(ip => new { ip.ProductId, ip.InquiryId });

            builder.Entity<InquiryProduct>()
                .HasOne(ip => ip.Inquiry)
                .WithMany(i => i.InquiryProducts)
                .HasForeignKey(ip => ip.InquiryId)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            builder.Entity<InquiryProduct>()
                .HasOne(ip => ip.Product)
                .WithMany()
                .HasForeignKey(ip => ip.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict
            builder.Entity<Inventory>()
                .HasOne(i => i.Product)
                 .WithMany(c => c.Inventories)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Change to Restrict

            builder.Entity<Inventory>()
                .HasOne(i => i.Location)
                .WithMany()
                .HasForeignKey(i => i.LocationId)
                .OnDelete(DeleteBehavior.Cascade); // Keep Cascade for LocationId


            builder.Entity<PurchaseOrder>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction); // Change to NoAction or Restrict
            builder.Entity<InventoryHistory>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.NoAction); // Change to NoAction or Restrict
            builder.Entity<InventoryHistory>()
                .HasOne(i => i.Location)
                .WithMany()
                .HasForeignKey(i => i.LocationId)
                .OnDelete(DeleteBehavior.NoAction); // Or DeleteBehavior.Restrict
            builder.Entity<PurchaseOrderItem>()
                .HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.NoAction); // Or DeleteBehavior.Restrict

            builder.Entity<PurchaseOrderItem>()
                .HasOne(p => p.UnitConversation)
                .WithMany()
                .HasForeignKey(p => p.UnitId)
                .OnDelete(DeleteBehavior.NoAction); // Or DeleteBehavior.Restrict

            builder.Entity<Supplier>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(e => e.BillingAddress)
                  .WithMany()
                  .HasForeignKey(rc => rc.BillingAddressId)
                  .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(e => e.ShippingAddress)
                  .WithMany()
                  .HasForeignKey(rc => rc.ShippingAddressId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ProductCategory>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<UnitConversation>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<EmailTemplate>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Reminder>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Expense>(b =>
            {
                b.HasOne(e => e.ExpenseBy)
                    .WithMany()
                    .HasForeignKey(rc => rc.ExpenseById)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(e => e.ExpenseTaxes)
                  .WithOne(c => c.Expense)
                  .HasForeignKey(rc => rc.ExpenseId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ExpenseCategory>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ProductTax>(b =>
            {
                b.HasKey(c => new { c.ProductId, c.TaxId });
            });

            builder.Entity<City>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Inventory>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Tax>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Product>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<ProductTax>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<InquirySource>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<InquiryStatus>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<InquiryProduct>(b =>
            {
                b.HasKey(c => new { c.ProductId, c.InquiryId });
            });

            builder.Entity<InquiryActivity>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<InquiryAttachment>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<InquiryNote>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Brand>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PurchaseOrderPayment>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PurchaseOrderItem>(b =>
            {
                b.HasOne(e => e.UnitConversation)
                    .WithMany()
                    .HasForeignKey(ur => ur.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SalesOrderItem>(b =>
            {
                b.HasOne(e => e.UnitConversation)
                    .WithMany()
                    .HasForeignKey(ur => ur.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<SalesOrderPayment>(b =>
            {
                b.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(ur => ur.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Data.Page>(b =>
            {
                // Each User can have many UserClaims
                b.HasMany(e => e.Actions)
                    .WithOne(e => e.Page)
                    .HasForeignKey(uc => uc.PageId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
            });

            builder.Entity<Location>(b =>
            {
                b.HasMany(e => e.UserLocations)
                    .WithOne(c => c.Location)
                    .HasForeignKey(ur => ur.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StockTransfer>(b =>
            {
                b.HasMany(e => e.StockTransferItems)
                   .WithOne(c => c.StockTransfer)
                   .HasForeignKey(ur => ur.StockTransferId)
                   .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(st => st.FromLocation)
                    .WithMany()
                    .HasForeignKey(st => st.FromLocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(st => st.ToLocation)
                    .WithMany()
                    .HasForeignKey(st => st.ToLocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(st => st.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(st => st.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            builder.Entity<User>().ToTable("Users");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<RoleClaim>().ToTable("RoleClaims");
            builder.Entity<UserClaim>().ToTable("UserClaims");
            builder.Entity<UserLogin>().ToTable("UserLogins");
            builder.Entity<UserRole>().ToTable("UserRoles");
            builder.Entity<UserToken>().ToTable("UserTokens");
            builder.DefalutMappingValue();
            builder.DefalutDeleteValueFilter();
        }
    }
}
