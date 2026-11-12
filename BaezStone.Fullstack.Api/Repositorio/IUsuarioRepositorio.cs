using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;

namespace BaezStone.Fullstack.Api.Repositorio;

public interface IUsuarioRepositorio
{
    Task<UsuarioLoginReadDto?> Register(Usuario usuario, string password);
    Task<UsuarioLoginReadDto?> Login(string userName, string password);
    Task<bool> Existe(string userName);
    Task<UsuarioReadDto?> GetUsuarioById(int id);
}
