using AutoMapper;
using DocumentDb.Pictures.Models;
using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentDb.Pictures.Mappings
{
    public class DocumentMappingProfile : Profile
    {
        public DocumentMappingProfile()
        {
            CreateMap<Document, PictureItem>()
                .ForMember(vm => vm.Id, map => map.MapFrom(doc => doc.GetPropertyValue<string>("id")))
                .ForMember(vm => vm.Title, map => map.MapFrom(doc => doc.GetPropertyValue<string>("title")))
                .ForMember(vm => vm.Category, map => map.MapFrom(doc => doc.GetPropertyValue<string>("category")))
                .ForMember(vm => vm.DateCreated, map => map.MapFrom(doc => doc.GetPropertyValue<DateTime>("dateCreated")));
        }
    }
}
