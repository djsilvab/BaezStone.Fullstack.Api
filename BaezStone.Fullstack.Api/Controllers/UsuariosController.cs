using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;
using BaezStone.Fullstack.Api.Repositorio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaezStone.Fullstack.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioRepositorio _userRepo;
    private readonly ResponseDto _response;

    public UsuariosController(IUsuarioRepositorio userRepo)
    {
        _userRepo = userRepo;
        _response = new();
    }

    [HttpPost("Login")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResponseDto>> Login([FromBody] UsuarioDto userDto)
    {

        var login = await _userRepo.Login(userDto.UserName, userDto.Password);

        if (login is null)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "Nombre de usuario o contraseña incorrectos";
            return Unauthorized(_response);
        }

        _response.Result = login;
        _response.DisplayMessage = "Login exitoso";
        return Ok(_response);

    }

    [HttpPost("Register")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseDto>> Register([FromBody] UsuarioDto usuarioDto)
    {
        var registro = await _userRepo.Register(new Usuario { UserName = usuarioDto.UserName }, usuarioDto.Password);

        if (registro is null)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "El usuario ya existe";
            return Conflict(_response);
        }

        _response.DisplayMessage = "Usuario registrado con éxito";
        _response.Result = registro;

        return CreatedAtAction(nameof(GetUsuario), new { id = registro.Id }, _response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<ActionResult<ResponseDto>> GetUsuario(int id)
    {
        var usuario = await _userRepo.GetUsuarioById(id);

        if (usuario is null)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "Usuario no encontrado";
            return NotFound(_response);
        }

        _response.Result = usuario;
        _response.DisplayMessage = "Usuario encontrado";
        return Ok(_response);
    }
}
