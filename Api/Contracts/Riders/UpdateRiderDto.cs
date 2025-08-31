using System.ComponentModel.DataAnnotations;
namespace Api.Contracts.Riders;

public class UpdateRiderDto
{
    /// <summary>Họ tên Rider</summary>
    public string? FullName { get; set; }
    /// <summary>Số điện thoại</summary>
    public string? Phone { get; set; }
    /// <summary>Ảnh đại diện URL</summary>
    [Url]
    public string? ImgUrl { get; set; }
}