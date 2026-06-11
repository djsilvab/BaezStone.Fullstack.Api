using System.ComponentModel.DataAnnotations;

namespace BaezStone.Fullstack.Api.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; }
        [Required]
        public bool Estado { get; set; }

    }
}
