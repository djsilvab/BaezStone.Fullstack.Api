namespace BaezStone.Fullstack.Api.Dtos;

public class UsuarioLoginReadDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
