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
    public class EmployeeController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;
        public EmployeeController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var employee = await this.db.Employees
                .Take(10)
                .ToListAsync();

            return Ok(employee);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var employee = await this.db.Employees.Include(e => e.EmployeeDepartmentHistories).Include(e => e.EmployeePayHistories)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);
            if (employee == null) return NotFound();

            var EmployeeDTO = this.mapper.Map<EmployeeDTO>(employee);

            return Ok(EmployeeDTO);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeWithPersonDTO employeeWithPersonDTO)
        {


            var be = new BusinessEntity();
            db.BusinessEntities.Add(be);
            await db.SaveChangesAsync();                 // identity generated
            var newId = be.BusinessEntityID;


            // 2) Person
            var person = new Person
            {
                BusinessEntityID = newId,
                PersonType = employeeWithPersonDTO.PersonType,             // e.g., "EM"
                FirstName = employeeWithPersonDTO.FirstName,
                LastName = employeeWithPersonDTO.LastName,
            };

            db.People.Add(person);
            await db.SaveChangesAsync();

            if (employeeWithPersonDTO.EmployeeDTO.HireDate == null)
                employeeWithPersonDTO.EmployeeDTO.HireDate = DateTime.Now;

            var employee = this.mapper.Map<Employee>(employeeWithPersonDTO.EmployeeDTO);
            employee.BusinessEntityID = newId;
            this.db.Employees.Add(employee);

            await this.db.SaveChangesAsync();

            var readEmployeeDTO = this.mapper.Map<EmployeeDTO>(employee);
            return CreatedAtAction(nameof(Get), new { Id = employee.BusinessEntityID }, readEmployeeDTO);
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

