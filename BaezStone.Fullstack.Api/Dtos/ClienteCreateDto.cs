using System.ComponentModel.DataAnnotations;

namespace BaezStone.Fullstack.Api.Dtos;

public class ClienteCreateDto
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Nombres { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Apellidos { get; set; } = string.Empty;
    [Required, MaxLength(200)]  public string Direccion { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Telefono { get; set; } = string.Empty;
}
