using System.ComponentModel.DataAnnotations;

namespace BaezStone.Fullstack.Api.Dtos;

public class MovieCreateDto
{
    [Required(ErrorMessage = "The movie name is required.")]
    [MinLength(3, ErrorMessage = "The movie name must be at least 3 characters long.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "The category ID is required.")]
    public int CategoryId { get; set; }

    [MaxLength(100, ErrorMessage = "The director name must be at most 100 characters long.")]
    public string? Director { get; set; }
}
