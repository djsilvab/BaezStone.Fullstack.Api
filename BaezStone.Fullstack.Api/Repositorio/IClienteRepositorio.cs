using BaezStone.Fullstack.Api.Dtos;

namespace BaezStone.Fullstack.Api.Repositorio;

public interface IClienteRepositorio
{
    Task<List<ClienteDto>> GetClientes();
    Task<ClienteDto> GetClienteById(int id);
    Task<ClienteDto> CreateUpdateCliente(ClienteDto clienteDto);
    Task<bool> DeleteCliente(int id);

}
