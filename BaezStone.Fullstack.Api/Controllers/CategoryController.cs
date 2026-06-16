using AutoMapper;
using BaezStone.Fullstack.Api.Data;
using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;

namespace BaezStone.Fullstack.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
    
        public CategoryController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CategoryReadDto>>> GetCategories()
            => Ok(await _context.Categories
                            .AsNoTracking()
                            .OrderBy(c => c.Nombre)
                            .ProjectTo<CategoryReadDto>(_mapper.ConfigurationProvider)
                            .ToListAsync());

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryReadDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                                         .AsNoTracking()
                                         .ProjectTo<CategoryReadDto>(_mapper.ConfigurationProvider)                                         
                                         .FirstOrDefaultAsync(c => c.Id == id);
            
            return category is null ? NotFound() : Ok(category);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CategoryReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CategoryReadDto>> PostCategory(CategoryCreateDto categoryDto)
        {
            var category = _mapper.Map<Category>(categoryDto);

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var readDto = _mapper.Map<CategoryReadDto>(category);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, readDto);
        }
    }
}
