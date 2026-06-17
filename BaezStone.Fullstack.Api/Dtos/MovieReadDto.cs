namespace BaezStone.Fullstack.Api.Dtos;

public class MovieReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryNombre { get; set; } = string.Empty;
    public string? Director { get; set; }

}
