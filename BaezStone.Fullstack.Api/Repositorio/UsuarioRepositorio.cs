using AutoMapper;
using AutoMapper.QueryableExtensions;
using BaezStone.Fullstack.Api.Data;
using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BaezStone.Fullstack.Api.Repositorio;

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;

    public UsuarioRepositorio(ApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<bool> Existe(string userName)
    {
        if (await _db.Usuarios.AnyAsync(u => u.UserName.ToLower() == userName.ToLower()))
        {
            return true;
        }
        return false;
    }

    public async Task<UsuarioReadDto?> GetUsuarioById(int id)
    {
        return await _db.Usuarios.AsNoTracking()
                        .Where(u => u.Id == id)
                        .ProjectTo<UsuarioReadDto>(_mapper.ConfigurationProvider)
                        .FirstOrDefaultAsync();
    }

    public async Task<string> Login(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public async Task<int> Register(Usuario usuario, string password)
    {
        if (await Existe(usuario.UserName)) return -1;

        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

        usuario.PasswordHash = passwordHash;
        usuario.PasswordSalt = passwordSalt;

        await _db.Usuarios.AddAsync(usuario);
        await _db.SaveChangesAsync();
        return usuario.Id;

    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new System.Security.Cryptography.HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }
}
