using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using AutoMapper;
using RhManagementApi.Models;

namespace RhManagementApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class OpeningController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;
        public OpeningController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var openings = await this.db.Openings
                .ToListAsync();

            return Ok(openings);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get (int id)
        {
            var opening = await this.db.Openings
                .FirstOrDefaultAsync(o => o.OpeningID == id);
            if (opening == null) return NotFound();

            var openingDTO = this.mapper.Map<OpeningDTO>(opening);

            return Ok(openingDTO);
        }

        [HttpPost]
        public async Task<IActionResult> Create(OpeningDTO openingDTO)
        {
            var opening = this.mapper.Map<Opening>(openingDTO);
            this.db.Openings.Add(opening);
            await this.db.SaveChangesAsync();

            var readOpeningDTO = this.mapper.Map<OpeningDTO>(opening);
            return CreatedAtAction(nameof(Get),new {Id = opening.OpeningID}, readOpeningDTO);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, EmployeeDTO employeeDTO)
        {
            if (id != employeeDTO.BusinessEntityID) return BadRequest();

            var employee = await this.db.Employees.Include(e => e.EmployeeDepartmentHistories).Include(e => e.EmployeePayHistories)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

            if (employee == null) return NotFound();

            if (employeeDTO.JobTitle != null) employee.JobTitle = employeeDTO.JobTitle;
            if (employeeDTO.BirthDate.HasValue) employee.BirthDate = employeeDTO.BirthDate.Value;
            if (employeeDTO.Gender != null) employee.Gender = employeeDTO.Gender;
            if (employeeDTO.MaritalStatus != null) employee.MaritalStatus = employeeDTO.MaritalStatus;
            if (employeeDTO.OrganizationLevel != null) employee.OrganizationLevel = employeeDTO.OrganizationLevel;
            if (employeeDTO.HireDate.HasValue) employee.HireDate = employeeDTO.HireDate.Value;

            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<EmployeeDTO>(employee));
        }
    }
}