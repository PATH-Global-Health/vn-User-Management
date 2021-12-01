using AutoMapper;
using Data.MongoCollections;
using Data.ViewModels;

namespace Service.MappingProfiles
{
    public class GroupMappingProfile : Profile
    {
        public GroupMappingProfile()
        {
            CreateMap<Group, GroupModel>().ReverseMap();
            CreateMap<Group, GroupOverviewModel>().ReverseMap();
            CreateMap<GroupUpdateModel, Group>().ReverseMap();
        }
    }
}
