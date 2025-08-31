
using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Riders;

public class UpdateRiderDto
{
    /// <example>Tran Thi B</example>
    [StringLength(200)]
    public string? FullName { get; set; }

    /// <example>+84 987 654 321</example>
    public string? Phone { get; set; }

    /// <example>https://cdn.example.com/avatar.png</example>
    [Url]
    public string? ImgUrl { get; set; }
}
