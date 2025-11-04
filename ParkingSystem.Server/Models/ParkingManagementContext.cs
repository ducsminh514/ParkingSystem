using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ParkingSystem.Server.Models;

public partial class ParkingManagementContext : DbContext
{
    public ParkingManagementContext()
    {
    }

    public ParkingManagementContext(DbContextOptions<ParkingManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerReport> CustomerReports { get; set; }

    public virtual DbSet<ParkingRegistration> ParkingRegistrations { get; set; }

    public virtual DbSet<ParkingSlot> ParkingSlots { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<ReportAttachment> ReportAttachments { get; set; }

    public virtual DbSet<ReportCategory> ReportCategories { get; set; }

    public virtual DbSet<ReportComment> ReportComments { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection string is provided via DI in Program.cs
        // Only configure if options are not already set
        if (!optionsBuilder.IsConfigured)
        {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
            // Fallback connection string (not used when DI is configured in Program.cs)
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ParkingManagement;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B84FB9EDB3");

            entity.ToTable("Customer");

            entity.Property(e => e.CustomerId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("CustomerID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CustomerReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Customer__D5BD48E5AE46D604");

            entity.ToTable("CustomerReport");

            entity.HasIndex(e => e.CreatedDate, "IDX_CustomerReport_CreatedDate").IsDescending();

            entity.HasIndex(e => e.CustomerId, "IDX_CustomerReport_CustomerID");

            entity.HasIndex(e => e.Status, "IDX_CustomerReport_Status");

            entity.Property(e => e.ReportId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("ReportID");
            entity.Property(e => e.AssignedStaffId).HasColumnName("AssignedStaffID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasDefaultValue("Normal");
            entity.Property(e => e.ResolvedDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.AssignedStaff).WithMany(p => p.CustomerReports)
                .HasForeignKey(d => d.AssignedStaffId)
                .HasConstraintName("FK__CustomerR__Assig__693CA210");

            entity.HasOne(d => d.Category).WithMany(p => p.CustomerReports)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CustomerR__Categ__68487DD7");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerReports)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CustomerR__Custo__6754599E");
        });

        modelBuilder.Entity<ParkingRegistration>(entity =>
        {
            entity.HasKey(e => e.RegistrationId).HasName("PK__ParkingR__6EF58830A5FDD2FF");

            entity.ToTable("ParkingRegistration");

            entity.Property(e => e.RegistrationId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("RegistrationID");
            entity.Property(e => e.CheckInTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CheckOutTime).HasColumnType("datetime");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("InUse");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");

            entity.HasOne(d => d.Slot).WithMany(p => p.ParkingRegistrations)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ParkingRe__SlotI__4BAC3F29");

            entity.HasOne(d => d.Staff).WithMany(p => p.ParkingRegistrations)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__ParkingRe__Staff__4CA06362");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.ParkingRegistrations)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ParkingRe__Vehic__4AB81AF0");
        });

        modelBuilder.Entity<ParkingSlot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PK__ParkingS__0A124A4F3EF8E1A0");

            entity.ToTable("ParkingSlot");

            entity.HasIndex(e => e.SlotCode, "UQ__ParkingS__38BD98CC66DC0609").IsUnique();

            entity.Property(e => e.SlotId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("SlotID");
            entity.Property(e => e.SlotCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Available");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A58DEDB11AA");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.RegistrationId).HasColumnName("RegistrationID");

            entity.HasOne(d => d.Registration).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RegistrationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__Registr__5165187F");
        });

        modelBuilder.Entity<ReportAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PK__ReportAt__442C64DED3AF58F8");

            entity.ToTable("ReportAttachment");

            entity.Property(e => e.AttachmentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("AttachmentID");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.UploadedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Report).WithMany(p => p.ReportAttachments)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReportAtt__Repor__6E01572D");
        });

        modelBuilder.Entity<ReportCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ReportCa__19093A2BB4B4E9F2");

            entity.ToTable("ReportCategory");

            entity.HasIndex(e => e.CategoryName, "UQ__ReportCa__8517B2E058DF3867").IsUnique();

            entity.Property(e => e.CategoryId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ReportComment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__ReportCo__C3B4DFAAD4B247F3");

            entity.ToTable("ReportComment");

            entity.HasIndex(e => e.ReportId, "IDX_ReportComment_ReportID");

            entity.Property(e => e.CommentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("CommentID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.UserType).HasMaxLength(20);

            entity.HasOne(d => d.Report).WithMany(p => p.ReportComments)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReportCom__Repor__73BA3083");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__96D4AAF7430F0852");

            entity.HasIndex(e => e.Username, "UQ__Staff__536C85E47E11BCED").IsUnique();

            entity.Property(e => e.StaffId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("StaffID");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(100);
            entity.Property(e => e.Shift).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__Vehicle__476B54B2C55C44F0");

            entity.ToTable("Vehicle");

            entity.HasIndex(e => e.PlateNumber, "UQ__Vehicle__03692624DA210416").IsUnique();

            entity.Property(e => e.VehicleId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("VehicleID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.PlateNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.VehicleType).HasMaxLength(50);

            entity.HasOne(d => d.Customer).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Vehicle__Custome__3C69FB99");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
