using System.ComponentModel.DataAnnotations;

namespace BaezStone.Fullstack.Api.Models;

public class Cliente
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Nombres { get; set; } = default!;
    [Required]
    public string Apellidos { get; set; } = default!;
    [Required]
    public string Direccion { get; set; } = default!;
    [Required]
    public string Telefono { get; set; } = default!;
}
