namespace Domain.Entities;

public enum UserRole { Admin, Company, Driver, Rider }
public enum ApplyStatus { Applied, Accepted, Rejected, Expired, Cancelled }
public enum OrderStatus { Pending, InProgress, Completed, Cancelled }
public enum TxStatus { Pending, Completed, Failed }

public enum TxType : byte
{
  Topup = 0,
  PaySalary = 1,
  PayMembership = 2,
  OrderPayment = 3,
  Refund = 4,
  Withdraw = 5
}

public class User
{
  public string Id { get; set; } = default!;
  public string Email { get; set; } = default!;
  public string PasswordHash { get; set; } = default!;
  public UserRole Role { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Company
{
  public string Id { get; set; } = default!;
  public string OwnerUserId { get; set; } = default!;
  public string Name { get; set; } = default!;
  public string? Description { get; set; }
  public string? ImgUrl { get; set; }              // avatar/logo
  public decimal Rating { get; set; }
  public string Membership { get; set; } = "Free";
  public DateTime? MembershipExpiresAt { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   // added to match schema
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;   // added to match schema
}

public class DriverProfile
{
  public string Id { get; set; } = default!;
  public string UserId { get; set; } = default!;
  public string FullName { get; set; } = default!;
  public string Email { get; set; } = default!;
  public string? Phone { get; set; }
  public string? Bio { get; set; }
  public string? ImgUrl { get; set; }              // avatar
  public decimal Rating { get; set; }
  public string? Skills { get; set; }              // JSON string
  public string? Location { get; set; }
  public bool IsAvailable { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   // added
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;   // added
}

public class RiderProfile
{
  public string Id { get; set; } = default!;
  public string UserId { get; set; } = default!;
  public string FullName { get; set; } = default!;
  public string? Phone { get; set; }
  public string? ImgUrl { get; set; }              // avatar
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   // added
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;   // added
}

public class Service
{
  public string Id { get; set; } = default!;
  public string CompanyId { get; set; } = default!;
  public string Title { get; set; } = default!;
  public string? Description { get; set; }
  public string? ImgUrl { get; set; }
  public int PriceCents { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   // added
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;   // added
}

public class Advertisement
{
  public string Id { get; set; } = default!;
  public string CompanyId { get; set; } = default!;
  public string Title { get; set; } = default!;
  public string? Description { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CompanyDriverRelation
{
  public string Id { get; set; } = default!;
  public string CompanyId { get; set; } = default!;
  public string DriverUserId { get; set; } = default!;
  public int BaseSalaryCents { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class JobApplication
{
  public string Id { get; set; } = default!;
  public string CompanyId { get; set; } = default!;
  public string DriverUserId { get; set; } = default!;
  public ApplyStatus Status { get; set; } = ApplyStatus.Applied;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime? ExpiresAt { get; set; }
}

public class Invite
{
  public string Id { get; set; } = default!;
  public string CompanyId { get; set; } = default!;
  public string DriverUserId { get; set; } = default!;
  public int BaseSalaryCents { get; set; }
  public string Status { get; set; } = "Pending";
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime? ExpiresAt { get; set; }
}

public class Order
{
  public string Id { get; set; } = default!;
  public string RiderUserId { get; set; } = default!;
  public string CompanyId { get; set; } = default!;
  public string ServiceId { get; set; } = default!;
  public OrderStatus Status { get; set; } = OrderStatus.Pending;
  public int PriceCents { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Review
{
  public string Id { get; set; } = default!;
  public string CompanyId { get; set; } = default!;
  public Company Company { get; set; } = default!;

  public string? OrderId { get; set; }           // now nullable
  public Order? Order { get; set; }

  public string RiderUserId { get; set; } = default!;
  public int Rating { get; set; }
  public string? Comment { get; set; }
  public DateTime CreatedAt { get; set; }
}

public class Wallet
{
  public string Id { get; set; } = default!;
  public string OwnerType { get; set; } = default!; // Company/Driver/Rider/Admin
  public string OwnerRefId { get; set; } = default!;
  public int BalanceCents { get; set; }
  public int LowBalanceThreshold { get; set; } = 10000;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Transaction
{
  public string Id { get; set; } = default!;
  public string? FromWalletId { get; set; }
  public string? ToWalletId { get; set; }
  public int AmountCents { get; set; }
  public TxStatus Status { get; set; } = TxStatus.Pending;
  public string? IdempotencyKey { get; set; }
  public TxType Type { get; set; } = TxType.Topup;
  public string? RefId { get; set; }

  // NEW: JSON metadata; make sure to serialize valid JSON
  public string? MetaJson { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
