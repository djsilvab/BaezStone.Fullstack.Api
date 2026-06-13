using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BaezStone.Fullstack.Api.Models
{
    public class Movie
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        [JsonIgnore]
        [ValidateNever]
        public Category Category { get; set; } = null!;
        [MaxLength(100)]
        public string? Director { get; set; }
    }
}
