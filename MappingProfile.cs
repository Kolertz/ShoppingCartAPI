using AutoMapper;
using ShoppingList.Entities;
using ShoppingList.Models;

namespace ShoppingList
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        { 
            CreateMap<Item, ItemDTO>(); // <source, target>
            CreateMap<ItemToChange, ItemDTO>().ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.OriginalQuantity));
        }
    }
}
