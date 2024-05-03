using Contracts;
using AutoMapper;
namespace SearchService.RequestsHelper;

public class MappingProfiles : Profile
{
    public MappingProfiles() 
    {
        CreateMap<AuctionCreated, Item>();
        CreateMap<AuctionUpdated, Item>();
    }
}
