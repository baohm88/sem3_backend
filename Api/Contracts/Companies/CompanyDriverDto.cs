namespace Api.Contracts.Companies;

/// <summary>Thông tin driver thuộc company</summary>
public class CompanyDriverDto
{
    public string RelationId { get; set; } = default!;
    public string DriverUserId { get; set; } = default!;

    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? ImgUrl { get; set; }

    public decimal Rating { get; set; }
    public int BaseSalaryCents { get; set; }
}
