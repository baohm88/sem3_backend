using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  public DbSet<User> Users => Set<User>();
  public DbSet<Company> Companies => Set<Company>();
  public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();
  public DbSet<RiderProfile> RiderProfiles => Set<RiderProfile>();
  public DbSet<Service> Services => Set<Service>();
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<Review> Reviews => Set<Review>();
  public DbSet<Wallet> Wallets => Set<Wallet>();
  public DbSet<Advertisement> Advertisements => Set<Advertisement>();
  public DbSet<CompanyDriverRelation> CompanyDriverRelations => Set<CompanyDriverRelation>();
  public DbSet<JobApplication> JobApplications => Set<JobApplication>();
  public DbSet<Invite> Invites => Set<Invite>();
  public DbSet<Transaction> Transactions => Set<Transaction>();

  protected override void OnModelCreating(ModelBuilder b)
  {
    // ===== users =====
    b.Entity<User>(e =>
    {
      e.ToTable("users");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.Email).HasColumnName("email");
      e.Property(x => x.PasswordHash).HasColumnName("password_hash");
      e.Property(x => x.Role).HasColumnName("role").HasConversion<string>(); // store enum as string
      e.Property(x => x.IsActive).HasColumnName("is_active");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== companies =====
    b.Entity<Company>(e =>
    {
      e.ToTable("companies");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
      e.Property(x => x.Name).HasColumnName("name");
      e.Property(x => x.Description).HasColumnName("description");
      e.Property(x => x.ImgUrl).HasColumnName("img_url"); // avatar/logo
      e.Property(x => x.Rating).HasColumnName("rating");
      e.Property(x => x.Membership).HasColumnName("membership"); // stored as string (DB enum or varchar)
      e.Property(x => x.MembershipExpiresAt).HasColumnName("membership_expires_at");
      e.Property(x => x.IsActive).HasColumnName("is_active");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== driver_profiles =====
    b.Entity<DriverProfile>(e =>
    {
      e.ToTable("driver_profiles");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.UserId).HasColumnName("user_id");
      e.Property(x => x.FullName).HasColumnName("full_name");
      e.Property(x => x.Email).HasColumnName("email");
      e.Property(x => x.Phone).HasColumnName("phone");
      e.Property(x => x.Bio).HasColumnName("bio");
      e.Property(x => x.ImgUrl).HasColumnName("img_url");  // avatar
      e.Property(x => x.Rating).HasColumnName("rating");
      e.Property(x => x.Skills).HasColumnName("skills").HasColumnType("json"); // JSON string column
      e.Property(x => x.Location).HasColumnName("location");
      e.Property(x => x.IsAvailable).HasColumnName("is_available");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== rider_profiles =====
    b.Entity<RiderProfile>(e =>
    {
      e.ToTable("rider_profiles");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.UserId).HasColumnName("user_id");
      e.Property(x => x.FullName).HasColumnName("full_name");
      e.Property(x => x.Phone).HasColumnName("phone");
      e.Property(x => x.ImgUrl).HasColumnName("img_url");  // avatar
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== services =====
    b.Entity<Service>(e =>
    {
      e.ToTable("services");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.CompanyId).HasColumnName("company_id");
      e.Property(x => x.Title).HasColumnName("title");
      e.Property(x => x.Description).HasColumnName("description");
      e.Property(x => x.ImgUrl).HasColumnName("img_url");
      e.Property(x => x.PriceCents).HasColumnName("price_cents");
      e.Property(x => x.IsActive).HasColumnName("is_active");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== orders =====
    b.Entity<Order>(e =>
    {
      e.ToTable("orders");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.RiderUserId).HasColumnName("rider_user_id");
      e.Property(x => x.CompanyId).HasColumnName("company_id");
      e.Property(x => x.ServiceId).HasColumnName("service_id");
      e.Property(x => x.Status).HasColumnName("status").HasConversion<string>(); // store enum as string
      e.Property(x => x.PriceCents).HasColumnName("price_cents");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== reviews =====
    // b.Entity<Review>(e =>
    // {
    //   e.ToTable("reviews");
    //   e.HasKey(x => x.Id);
    //   e.Property(x => x.Id).HasColumnName("id");
    //   e.Property(x => x.OrderId).HasColumnName("order_id");
    //   e.Property(x => x.RiderUserId).HasColumnName("rider_user_id");
    //   e.Property(x => x.Rating).HasColumnName("rating");
    //   e.Property(x => x.Comment).HasColumnName("comment");
    //   e.Property(x => x.CreatedAt).HasColumnName("created_at");
    // });
    // ===== reviews =====
    // ===== reviews =====
    b.Entity<Review>(e =>
    {
      e.ToTable("reviews");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");

      e.Property(x => x.CompanyId).HasColumnName("company_id"); // NEW: bắt buộc
      e.Property(x => x.OrderId).HasColumnName("order_id");     // nullable
      e.Property(x => x.RiderUserId).HasColumnName("rider_user_id");
      e.Property(x => x.Rating).HasColumnName("rating");
      e.Property(x => x.Comment).HasColumnName("comment");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");

      // Khai báo quan hệ giống DB:
      e.HasOne(r => r.Company)
      .WithMany()                                  // (chưa cần collection)
      .HasForeignKey(r => r.CompanyId)
      .OnDelete(DeleteBehavior.Cascade);

      e.HasOne(r => r.Order)
      .WithMany()
      .HasForeignKey(r => r.OrderId)
      .OnDelete(DeleteBehavior.SetNull);
    });



    // ===== wallets =====
    b.Entity<Wallet>(e =>
    {
      e.ToTable("wallets");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.OwnerType).HasColumnName("owner_type"); // Company/Driver/Rider/Admin
      e.Property(x => x.OwnerRefId).HasColumnName("owner_ref_id");
      e.Property(x => x.BalanceCents).HasColumnName("balance_cents");
      e.Property(x => x.LowBalanceThreshold).HasColumnName("low_balance_threshold");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== advertisements =====
    b.Entity<Advertisement>(e =>
    {
      e.ToTable("advertisements");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.CompanyId).HasColumnName("company_id");
      e.Property(x => x.Title).HasColumnName("title");
      e.Property(x => x.Description).HasColumnName("description");
      e.Property(x => x.IsActive).HasColumnName("is_active");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== company_driver_relations =====
    b.Entity<CompanyDriverRelation>(e =>
    {
      e.ToTable("company_driver_relations");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.CompanyId).HasColumnName("company_id");
      e.Property(x => x.DriverUserId).HasColumnName("driver_user_id");
      e.Property(x => x.BaseSalaryCents).HasColumnName("base_salary_cents");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    });

    // ===== job_applications =====
    b.Entity<JobApplication>(e =>
    {
      e.ToTable("job_applications");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.CompanyId).HasColumnName("company_id");
      e.Property(x => x.DriverUserId).HasColumnName("driver_user_id");
      e.Property(x => x.Status).HasColumnName("status").HasConversion<string>(); // store enum as string
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
    });

    // ===== invites =====
    b.Entity<Invite>(e =>
    {
      e.ToTable("invites");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.CompanyId).HasColumnName("company_id");
      e.Property(x => x.DriverUserId).HasColumnName("driver_user_id");
      e.Property(x => x.BaseSalaryCents).HasColumnName("base_salary_cents");
      e.Property(x => x.Status).HasColumnName("status");
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
      e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
    });

    // ===== transactions =====
    b.Entity<Transaction>(e =>
    {
      e.ToTable("transactions");
      e.HasKey(x => x.Id);
      e.Property(x => x.Id).HasColumnName("id");
      e.Property(x => x.FromWalletId).HasColumnName("from_wallet_id");
      e.Property(x => x.ToWalletId).HasColumnName("to_wallet_id");
      e.Property(x => x.AmountCents).HasColumnName("amount_cents");
      e.Property(x => x.Status).HasColumnName("status").HasConversion<string>(); // store enum as string
      e.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
      e.Property(x => x.Type).HasColumnName("type");                     // TxType (byte) persisted as numeric by default
      e.Property(x => x.RefId).HasColumnName("ref_id").HasMaxLength(255);
      e.Property(x => x.MetaJson).HasColumnName("meta_json");            // arbitrary JSON metadata
      e.Property(x => x.CreatedAt).HasColumnName("created_at");
    });
  }
}
