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

            CreateMap<Cliente, ClienteReadDto>()
                .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(src => src.Nombres+" "+ src.Apellidos));

            CreateMap<ClienteCreateDto, Cliente>()                
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());
        }
    }
}
