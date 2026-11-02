using BaezStone.Fullstack.Api.Dtos;

namespace BaezStone.Fullstack.Api.Repositorio;

public class ClienteRepositorio : IClienteRepositorio
{
    public Task<ClienteDto> CreateUpdateCliente(ClienteDto clienteDto)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteCliente(int id)
    {
        throw new NotImplementedException();
    }

    public Task<ClienteDto> GetClienteById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<List<ClienteDto>> GetClientes()
    {
        throw new NotImplementedException();
    }
}
