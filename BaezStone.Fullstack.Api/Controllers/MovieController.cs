using AutoMapper;
using AutoMapper.QueryableExtensions;
using BaezStone.Fullstack.Api.Data;
using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaezStone.Fullstack.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MovieController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MovieReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MovieReadDto>>> GetMovies()
            => Ok(await _context.Movies
                             .AsNoTracking()
                             .OrderBy(m => m.Nombre)
                             .ProjectTo<MovieReadDto>(_mapper.ConfigurationProvider)
                              .ToListAsync());

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(MovieReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MovieReadDto>> GetMovie(int id)
        {
            var movieDto = await _context.Movies.AsNoTracking()
                                             .ProjectTo<MovieReadDto>(_mapper.ConfigurationProvider)
                                             .FirstOrDefaultAsync(c => c.Id == id);
            return movieDto is null ? NotFound() : Ok(movieDto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(MovieReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MovieReadDto>> PostMovie(MovieCreateDto movieDto)
        {
            var movie = _mapper.Map<Movie>(movieDto);

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            await _context.Entry(movie).Reference(m => m.Category).LoadAsync();
            var readDto = _mapper.Map<MovieReadDto>(movie);

            return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, readDto);
        }
    }
}
