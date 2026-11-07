using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BaezStone.Fullstack.Api.Models;

[Index(nameof(UserName), IsUnique = true)]
public class Usuario
{
    [Key]
    public int Id { get; set; }
    [Required,MaxLength(50)]
    public string UserName { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = default!;
    public byte[] PasswordSalt { get; set; } = default!;
}
