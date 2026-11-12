using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;

namespace BaezStone.Fullstack.Api.Repositorio;

public interface IUsuarioRepositorio
{
    Task<UsuarioTokenReadDto?> Register(Usuario usuario, string password, CancellationToken cancellationToken);
    Task<UsuarioTokenReadDto?> Login(string userName, string password, CancellationToken cancellationToken);
    Task<bool> Existe(string userName, CancellationToken cancellationToken = default);
    Task<UsuarioReadDto?> GetUsuarioById(int id, CancellationToken cancellationToken);
}
