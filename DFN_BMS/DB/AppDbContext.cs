using DFN_BMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DFN_BMS.DB
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<UserMaster> UserMasters { get; set; }
        public DbSet<DepartmentMaster> DepartmentMasters { get; set; }
        public DbSet<UserPrivilege> UserPrivileges { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<MailSettings> MAIL_SETTINGS { get; set; }
        public DbSet<ItemGroupMaster> ItemGroupMasters { get; set; }
        public DbSet<ItemMaster> ItemMasters { get; set; }
        public DbSet<ItemTypeMaster> ItemTypeMasters { get; set; }
        public DbSet<SupplierGroupMaster> SupplierGroupMasters { get; set; }
        public DbSet<SupplierMaster> SupplierMasters { get; set; }
        public DbSet<CustomerGroupMaster> CustomerGroupMasters { get; set; }
        public DbSet<CustomerMaster> CustomerMasters { get; set; }

        public DbSet<PriceMaster> PriceMasters { get; set; }
    }
}
