using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SnackRack.Pages.Features.Orders;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(p =>
        {
            p.ToTable("products");

            p.HasKey(x => x.Id);

            p.Property(x => x.Id)
                .ValueGeneratedNever();

            p.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            p.Property(x => x.Price)
                .HasColumnType("numeric(12,2)")
                .IsRequired();

            p.HasIndex(x => x.Name);
        });

        modelBuilder.HasPostgresExtension("uuid-ossp");

        // ---------- Customer ----------
        modelBuilder.Entity<Customer>(c =>
        {
            c.ToTable("customers");

            c.HasKey(x => x.Id);

            c.Property(x => x.Id)
                .ValueGeneratedNever();

            c.Property(x => x.Name)
                .HasMaxLength(800);

            c.Property(x => x.Email)
                .HasMaxLength(800);

            c.Property(x => x.PhoneNumber)
                .HasMaxLength(800);

            c.HasIndex(x => x.Email);
        });

        // ---------- Order ----------
        modelBuilder.Entity<Order>(o =>
        {
            o.ToTable("orders");

            o.HasKey(x => x.Id);

            o.Property(x => x.Id)
                .ValueGeneratedNever();
            o.Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(OrderStatus.Pending)
                .IsRequired();
                

            o.HasOne(x => x.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey("CustomerId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            o.OwnsMany(x => x.OrderItems, item =>
            {
                item.ToTable("order_items");

                item.Property<int>("Id");
                item.HasKey("Id");

                item.WithOwner()
                    .HasForeignKey("OrderId");

                item.Property(i => i.ProductId).IsRequired();
                item.Property(i => i.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                item.Property(i => i.Price)
                    .HasColumnType("numeric(12,2)")
                    .IsRequired();

                item.Property(i => i.Quantity).IsRequired();

                item.HasIndex(i => i.ProductId);
            });
        });
    }
}