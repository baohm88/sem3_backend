
using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Riders;

public class ReviewOrderDto
{
    /// <example>5</example>
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <example>Very satisfied!</example>
    public string? Comment { get; set; }
}
