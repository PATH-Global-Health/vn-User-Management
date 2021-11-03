using AutoMapper;
using Data.MongoCollections;
using Data.ViewModels;

namespace Service.MappingProfiles
{
    public class GroupMappingProfile : Profile
    {
        public GroupMappingProfile()
        {
            CreateMap<Group, GroupModel>();
            CreateMap<Group, GroupOverviewModel>();
            CreateMap<GroupUpdateModel, Group>();

        }
    }
}
