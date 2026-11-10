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
    private readonly IConfiguration _configuration;

    public UsuarioRepositorio(ApplicationDbContext db, IMapper mapper, IConfiguration configuration)
    {
        _db = db;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<bool> Existe(string userName)
        => await _db.Usuarios.AsNoTracking().AnyAsync(u => u.UserName == userName);

    public async Task<UsuarioReadDto?> GetUsuarioById(int id)
    {
        return await _db.Usuarios.AsNoTracking()
                        .Where(u => u.Id == id)
                        .ProjectTo<UsuarioReadDto>(_mapper.ConfigurationProvider)
                        .FirstOrDefaultAsync();
    }

    public async Task<UsuarioReadDto?> Login(string userName, string password)
    {
        var usuario = await _db.Usuarios.AsNoTracking()
                                        .FirstOrDefaultAsync(u => u.UserName == userName);

        if (usuario is null) return null;

        return VerificarPasswordHash(password, usuario.PasswordHash, usuario.PasswordSalt)
                    ? _mapper.Map<UsuarioReadDto>(usuario) : null;
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
        using var hmac = new System.Security.Cryptography.HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
    }

    private bool VerificarPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt);
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
    }
}
