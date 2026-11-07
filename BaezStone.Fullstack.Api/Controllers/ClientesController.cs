using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Repositorio;
using Microsoft.AspNetCore.Mvc;

namespace BaezStone.Fullstack.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientesController : ControllerBase
{
    private readonly IClienteRepositorio _clienteRepositorio;
    private readonly ResponseDto _response;

    public ClientesController(IClienteRepositorio clienteRepositorio)
    {
        _clienteRepositorio = clienteRepositorio;
        _response = new ();
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto>> GetClientes()
    {

        var lista = await _clienteRepositorio.GetClientes();
        _response.Result = lista;
        _response.DisplayMessage = "Lista de clientes";

        return Ok(_response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto>> GetCliente(int id)
    {
        var cliente = await _clienteRepositorio.GetClienteById(id);

        if (cliente is null)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "Cliente no encontrado";
            return NotFound(_response);
        }

        _response.Result = cliente;
        _response.DisplayMessage = "Cliente encontrado";
        return Ok(_response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResponseDto>> PostCliente([FromBody] ClienteCreateDto clienteDto)
    {
        if (clienteDto.Id != 0)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "El ID del cliente debe ser 0 para crear un nuevo cliente";
            return BadRequest(_response);
        }

        var model = await _clienteRepositorio.CreateUpdateCliente(clienteDto);
        if (model is null)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "Error al crear el cliente";
            return StatusCode(StatusCodes.Status500InternalServerError, _response);
        }

        _response.Result = model;
        _response.DisplayMessage = "Cliente creado correctamente";

        return CreatedAtAction(nameof(GetCliente), new { id = model.Id }, _response);

    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto>> PutCliente(int id, [FromBody] ClienteCreateDto clienteDto)
    {
        if (id <= 0 || id != clienteDto.Id)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "ID del cliente no es válido o no coincide";
            return BadRequest(_response);
        }      

        var updatedCliente = await _clienteRepositorio.CreateUpdateCliente(clienteDto);
        if (updatedCliente is null)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "Cliente no encontrado";
            return NotFound(_response);
        }

        _response.Result = updatedCliente;
        _response.DisplayMessage = "Cliente actualizado correctamente";


        return Ok(_response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto>> DeleteCliente(int id)
    {
        var isSuccess = await _clienteRepositorio.DeleteCliente(id);
        if (!isSuccess)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "Cliente no encontrado";
            return NotFound(_response);
        }
       
        _response.DisplayMessage = "Cliente eliminado correctamente";
        return Ok(_response);

    }
}
