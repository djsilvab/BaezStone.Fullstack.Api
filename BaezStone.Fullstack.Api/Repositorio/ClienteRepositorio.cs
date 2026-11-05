using AutoMapper;
using AutoMapper.QueryableExtensions;
using BaezStone.Fullstack.Api.Data;
using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BaezStone.Fullstack.Api.Repositorio;

public class ClienteRepositorio : IClienteRepositorio
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;

    public ClienteRepositorio(ApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<ClienteReadDto?> CreateUpdateCliente(ClienteCreateDto clienteDto)
    {
        Cliente cliente;

        if(clienteDto.Id == 0)
        {
            cliente = _mapper.Map<Cliente>(clienteDto);
            cliente.FechaCreacion = DateTime.UtcNow;            
            await _db.Clientes.AddAsync(cliente);
        }
        else
        {
            var existente = await _db.Clientes.FindAsync(clienteDto.Id);
            if (existente is null) return null;

            _mapper.Map(clienteDto, existente);
            existente.FechaActualizacion = DateTime.UtcNow;
            cliente = existente;
        }

        await _db.SaveChangesAsync();
        return _mapper.Map<ClienteReadDto>(cliente);

    }

    public async Task<bool> DeleteCliente(int id)
    {       
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return false;

        _db.Clientes.Remove(cliente);
        await _db.SaveChangesAsync();
        return true;       
    }

    public async Task<ClienteReadDto?> GetClienteById(int id)
    {
        return await _db.Clientes.AsNoTracking()
                                 .Where(c => c.Id.Equals(id))
                                 .ProjectTo<ClienteReadDto>(_mapper.ConfigurationProvider)
                                 .FirstOrDefaultAsync();
    }

    public async Task<List<ClienteReadDto>> GetClientes()
    {
        return await _db.Clientes
                        .AsNoTracking()
                        .OrderBy(c => c.Apellidos)
                        .ThenBy(c => c.Nombres)
                        .ProjectTo<ClienteReadDto>(_mapper.ConfigurationProvider)
                        .ToListAsync();
    }
}
