using System.ComponentModel.DataAnnotations;

namespace BaezStone.Fullstack.Api.Models;

public class Cliente
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Nombres { get; set; } = default!;
    [Required, MaxLength(100)]
    public string Apellidos { get; set; } = default!;
    [Required, MaxLength(200)]
    public string Direccion { get; set; } = default!;
    [Required, MaxLength(20)]
    public string Telefono { get; set; } = default!;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
