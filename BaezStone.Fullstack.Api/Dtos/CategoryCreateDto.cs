using System.ComponentModel.DataAnnotations;

namespace BaezStone.Fullstack.Api.Dtos
{
    public class CategoryCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
    }
}
