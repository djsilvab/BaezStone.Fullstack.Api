using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaezStone.Fullstack.Api.Models
{
    public class Movie
    {        
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }
        public int CategoryId { get; set; }        
        public Category Category { get; set; }
    }
}
