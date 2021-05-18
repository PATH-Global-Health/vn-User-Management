using AutoMapper;
using Data.MongoCollections;
using Data.ViewModels;

namespace Service.MappingProfiles
{
    public class UserProfile:Profile
    {
        public UserProfile()
        {
            CreateMap<UserProfile, UserProfileViewModel>();
            CreateMap<UserProfileCreateModel, UserProfile>();
            CreateMap<AddressCreateModel, Address>();
        }
    }
}
