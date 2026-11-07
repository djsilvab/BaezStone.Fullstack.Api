using System.ComponentModel.DataAnnotations;

namespace BaezStone.Fullstack.Api.Dtos;

public class UsuarioDto
{
    [Required(ErrorMessage = "UserName es requerido"), 
        MinLength(4, ErrorMessage = "UserName debe tener al menos 4 caracteres"), 
        MaxLength(50)]
    public string UserName { get; set; } = string.Empty;
    [Required, MinLength(6, ErrorMessage = "Password debe tener al menos 6 caracteres"), MaxLength(50)]
    public string Password { get; set; } = default!;    
}
