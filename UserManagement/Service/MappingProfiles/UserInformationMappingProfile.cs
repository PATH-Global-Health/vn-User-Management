using AutoMapper;
using Data.MongoCollections;
using Data.ViewModels;
namespace Service.MappingProfiles
{
    public class UserInformationMappingProfile : Profile
    {
        public UserInformationMappingProfile()
        {
            CreateMap<UserInformation, UserInformationModel>()
                .ForMember(i => i.IsDisabled, map => map.MapFrom(dm => dm.IsDisabled.HasValue ? dm.IsDisabled.Value : false));
        }
    }
}
