
using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Riders;

public class CreateOrderDto
{
    /// <example>68a000000000000000000020</example>
    [Required]
    public string? CompanyId { get; set; }

    /// <example>68a0000000000000000000A0</example>
    [Required]
    public string? ServiceId { get; set; }
}
