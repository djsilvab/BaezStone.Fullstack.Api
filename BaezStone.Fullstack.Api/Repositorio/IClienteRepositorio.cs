using BaezStone.Fullstack.Api.Dtos;

namespace BaezStone.Fullstack.Api.Repositorio;

public interface IClienteRepositorio
{
    Task<List<ClienteReadDto>> GetClientes();
    Task<ClienteReadDto?> GetClienteById(int id);
    Task<ClienteReadDto?> CreateUpdateCliente(ClienteCreateDto clienteDto);
    Task<bool> DeleteCliente(int id);

}
