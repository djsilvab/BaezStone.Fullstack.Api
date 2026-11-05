using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;
using BaezStone.Fullstack.Api.Repositorio;
using Microsoft.AspNetCore.Mvc;

namespace BaezStone.Fullstack.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientesController : ControllerBase
{
    private readonly IClienteRepositorio _clienteRepositorio;
    protected ResponseDto _response;

    public ClientesController(IClienteRepositorio clienteRepositorio)
    {
        _clienteRepositorio = clienteRepositorio;
        _response = new ResponseDto();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
    {
        try
        {
            var lista = await _clienteRepositorio.GetClientes();
            _response.Result = lista;
            _response.DisplayMessage = "Lista de clientes";

        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> { ex.ToString() };
        }

        return Ok(_response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cliente>> GetCliente(int id)
    {
        var cliente = await _clienteRepositorio.GetClienteById(id);

        if (cliente == null)
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
    public async Task<ActionResult<ClienteReadDto>> PostCliente([FromBody] ClienteCreateDto clienteDto)
    {
        try
        {
            var model = await _clienteRepositorio.CreateUpdateCliente(clienteDto);
            _response.Result = model;
            _response.DisplayMessage = "Cliente creado correctamente";

            return CreatedAtAction("GetCliente", new { id = model.Id }, _response);
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> { ex.ToString() };
            _response.DisplayMessage = "Error al crear el cliente";
            return BadRequest(_response);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCliente(int id, [FromBody] ClienteCreateDto clienteDto)
    {
        if (id != clienteDto.Id)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "ID del cliente no coincide";
            return BadRequest(_response);
        }

        try
        {
            var updatedCliente = await _clienteRepositorio.CreateUpdateCliente(clienteDto);
            if (updatedCliente is null)
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Cliente no encontrado";
                return NotFound(_response);
            }

            _response.Result = updatedCliente;
            _response.DisplayMessage = "Cliente actualizado correctamente";
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "Error al actualizar el cliente";
            _response.ErrorMessages = new List<string> { ex.ToString() };
            return BadRequest(_response);
        }

        return Ok(_response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCliente(int id)
    {
        try
        {
            var isSuccess = await _clienteRepositorio.DeleteCliente(id);
            if (!isSuccess)
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Cliente no eliminado";
                return BadRequest(_response);
            }

            _response.Result = isSuccess;
            _response.DisplayMessage = "Cliente eliminado correctamente";
            return Ok(_response);
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.DisplayMessage = "Error al eliminar el cliente";
            _response.ErrorMessages = new List<string> { ex.ToString() };
            return BadRequest(_response);
        }
    }
}
