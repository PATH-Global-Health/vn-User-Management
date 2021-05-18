using AutoMapper;
using Data.MongoCollections;
using Data.ViewModels;
namespace Service.MappingProfiles
{
    public class UserInformationMappingProfile : Profile
    {
        public UserInformationMappingProfile()
        {
            CreateMap<UserInformation, UserInformationModel>();
        }
    }
}
