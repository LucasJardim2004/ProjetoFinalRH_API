using AutoMapper;
using RhManagementApi.Models;
using RhManagementApi.DTOs;

namespace Livraria.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CandidateInfo, CandidateInfoDTO>().ReverseMap();
            CreateMap<Department, DepartmentDTO>().ReverseMap();
            CreateMap<Employee, EmployeeDTO>().ReverseMap();
            CreateMap<EmployeeDepartmentHistory, EmployeeDepartmentHistoryDTO>().ReverseMap();
            CreateMap<EmployeePayHistory, EmployeePayHistoryDTO>().ReverseMap();
            CreateMap<JobCandidate, JobCandidateDTO>().ReverseMap();
            CreateMap<Login, LoginDTO>().ReverseMap();
            CreateMap<Opening, OpeningDTO>().ReverseMap();
            CreateMap<Person, PersonDTO>().ReverseMap();
        }
    }
}