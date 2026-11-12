using AutoMapper;
using AutoMapper.QueryableExtensions;
using BaezStone.Fullstack.Api.Data;
using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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

    public async Task<bool> Existe(string userName, CancellationToken cancellationToken = default)
        => await _db.Usuarios.AsNoTracking().AnyAsync(u => u.UserName == userName, cancellationToken);

    public async Task<UsuarioReadDto?> GetUsuarioById(int id, CancellationToken cancellationToken)
    {
        return await _db.Usuarios.AsNoTracking()
                        .Where(u => u.Id == id)
                        .ProjectTo<UsuarioReadDto>(_mapper.ConfigurationProvider)
                        .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UsuarioTokenReadDto?> Login(string userName, string password, CancellationToken cancellationToken)
    {
        var usuario = await _db.Usuarios.AsNoTracking()
                                        .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

        if (usuario is null) return null;

        if (!VerificarPasswordHash(password, usuario.PasswordHash, usuario.PasswordSalt)) return null;

        var login = _mapper.Map<UsuarioTokenReadDto>(usuario);
        login.Token = CrearToken(usuario);
        return login;
    }

    public async Task<UsuarioTokenReadDto?> Register(Usuario usuario, string password, CancellationToken cancellationToken)
    {
        if (await Existe(usuario.UserName, cancellationToken)) return null;

        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

        usuario.PasswordHash = passwordHash;
        usuario.PasswordSalt = passwordSalt;

        try
        {
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when(EsClaveDuplicada(ex))
        {
            return null;
        }

        var registro = _mapper.Map<UsuarioTokenReadDto>(usuario);
        registro.Token = CrearToken(usuario);
        return registro;
    }

    private static bool EsClaveDuplicada(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2627 or 2601 };

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private bool VerificarPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512(passwordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
    }

    private string CrearToken(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.UserName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value
            ?? throw new InvalidOperationException("AppSettings:Token is not configured.")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            Issuer = _configuration.GetSection("AppSettings:Issuer").Value
                        ?? throw new InvalidOperationException("AppSettings:Issuer no está configurado."),
            Audience = _configuration.GetSection("AppSettings:Audience").Value
                        ?? throw new InvalidOperationException("AppSettings:Audience no está configurado."),
            SigningCredentials = creds
        };

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
