using AutoMapper;
using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Models;

namespace BaezStone.Fullstack.Api.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Category, CategoryReadDto>();
            CreateMap<CategoryCreateDto, Category>();

            CreateMap<Movie, MovieReadDto>();
            CreateMap<MovieCreateDto, Movie>();
        }
    }
}
