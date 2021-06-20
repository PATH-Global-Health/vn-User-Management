using AutoMapper;
using Data.MongoCollections;
using Data.ViewModels;
using System.Linq;

namespace Service.MappingProfiles
{
    public class ApiModuleMappingProfile : Profile
    {
        public ApiModuleMappingProfile()
        {
            CreateMap<ApiModule, ApiModuleDetailModel>()
                .ForMember(m => m.Paths, map => map.MapFrom(dm => dm.Paths.Where(i => !i.IsDeleted).ToList()));
            CreateMap<ApiModule, ApiModuleModel>();
            CreateMap<ApiPath, ApiPathModel>();
        }
    }
}
