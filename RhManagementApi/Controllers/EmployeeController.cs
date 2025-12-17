using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using AutoMapper;
using RhManagementApi.Models;
using Microsoft.AspNetCore.Authorization;

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
                .Take(100)
                .ToListAsync();
 
            return Ok(employee);
        }
 
 
        [HttpGet("{id}")]
        // [Authorize]
        public async Task<IActionResult> Get(int id)
        {
            // 1) Load Employee with its own relationships
            var employee = await db.Employees
                .Include(e => e.EmployeeDepartmentHistories)
                .Include(e => e.EmployeePayHistories)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);
 
            if (employee == null) return NotFound();
 
            // 2) Load single phone and single email by BusinessEntityID
            // Use your actual DbSet property names here:
            // - If it's db.PersonPhones, change db.Phones to db.PersonPhones
            // - If it's db.PersonEmailAddresses, change db.EmailAddresses accordingly
 
            var phoneNumber = await db.PeoplePhones
                .Where(ph => ph.BusinessEntityID == id)
                .Select(ph => ph.PhoneNumber)
                .FirstOrDefaultAsync();
 
            var emailAddress = await db.EmailAddresses
                .Where(em => em.BusinessEntityID == id)
                .Select(em => em.EmailAddress)
                .FirstOrDefaultAsync();
 
            // 3) Map employee to DTO and attach single phone/email
            var dto = this.mapper.Map<EmployeeDTO>(employee);
 
            // Ensure your EmployeeDTO has *single* fields (string?) not lists:
            // public string? PhoneNumber { get; set; }
            // public string? EmailAddress { get; set; }
 
            return Ok(new
            {
                Employee = dto,
                PhoneNumber = phoneNumber,
                EmailAddress = emailAddress
            });
 
        }
 
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeWithPersonDTO dto)
        {
            if (dto == null) return BadRequest("Request body is required.");
            if (dto.EmployeeDTO == null) return BadRequest("EmployeeDTO is required.");
            if (string.IsNullOrWhiteSpace(dto.PersonType)) return BadRequest("PersonType is required.");
            if (string.IsNullOrWhiteSpace(dto.FirstName)) return BadRequest("FirstName is required.");
            if (string.IsNullOrWhiteSpace(dto.LastName)) return BadRequest("LastName is required.");
            if (string.IsNullOrWhiteSpace(dto.EmailAddress)) return BadRequest("EmailAddress is required.");
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) return BadRequest("PhoneNumber is required.");
            if (dto.DepartmentId <= 0) return BadRequest("DepartmentId is required and must be positive.");
 
            // AdventureWorks datetime lower bound (use 0001-01-01 for datetime2)
            var sqlLowerBound = new DateTime(1753, 1, 1);
            var sqlUpperBound = new DateTime(9999, 12, 31);
 
            var todayUtcDate = DateTime.UtcNow.Date;
            if (dto.EmployeeDTO.HireDate.HasValue &&
                (dto.EmployeeDTO.HireDate.Value <= sqlLowerBound || dto.EmployeeDTO.HireDate.Value >= sqlUpperBound))
                return BadRequest($"HireDate must be between {sqlLowerBound:yyyy-MM-dd} and {sqlUpperBound:yyyy-MM-dd}.");
 
            if (dto.EmployeeDTO.BirthDate.HasValue &&
                (dto.EmployeeDTO.BirthDate.Value <= sqlLowerBound || dto.EmployeeDTO.BirthDate.Value >= sqlUpperBound))
                return BadRequest($"BirthDate must be between {sqlLowerBound:yyyy-MM-dd} and {sqlUpperBound:yyyy-MM-dd}.");
 
            if (dto.EmployeeDTO.BirthDate.HasValue && dto.EmployeeDTO.BirthDate.Value.Date > todayUtcDate)
                return BadRequest("BirthDate cannot be in the future.");
 
            if (dto.EmployeeDTO.HireDate.HasValue && dto.EmployeeDTO.BirthDate.HasValue &&
                dto.EmployeeDTO.BirthDate.Value.Date > dto.EmployeeDTO.HireDate.Value.Date)
                return BadRequest("BirthDate cannot be after HireDate.");
 
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                // 1) BusinessEntity
                var be = new BusinessEntity();
                db.BusinessEntities.Add(be);
                await db.SaveChangesAsync(); // identity generated
                var newId = be.BusinessEntityID;
 
                // 2) Person (ensure PersonType is a valid 2-char code in AdventureWorks, e.g., "EM")
                var personType = dto.PersonType.Trim();
                if (personType.Length != 2)
                    return BadRequest("PersonType must be 2 characters (e.g., 'EM').");
 
                var person = new Person
                {
                    BusinessEntityID = newId,
                    PersonType       = personType,
                    FirstName        = dto.FirstName.Trim(),
                    LastName         = dto.LastName.Trim(),
                    // set other required fields if your model enforces them
                };
                db.People.Add(person);
                await db.SaveChangesAsync(); // ✅ Persist Person before Email/Phone to satisfy FK
 
                // 3) Email (dependent on Person)
                var email = new PersonEmailAddress
                {
                    BusinessEntityID = newId,
                    EmailAddress     = dto.EmailAddress.Trim()
                };
                db.EmailAddresses.Add(email);
 
                // 4) Phone (dependent on Person)
                var phone = new PersonPhone
                {
                    BusinessEntityID  = newId,
                    PhoneNumber       = dto.PhoneNumber.Trim(),
                    PhoneNumberTypeID = 1
                };
                // Use your actual DbSet name (e.g., db.Phones or db.PersonPhones)
                db.PeoplePhones.Add(phone);
 
                // 5) Employee
                var employee = new Employee
                {
                    BusinessEntityID  = newId,
                    JobTitle          = dto.EmployeeDTO.JobTitle,
                    NationalIDNumber  = dto.EmployeeDTO.NationalIDNumber,
                    BirthDate         = dto.EmployeeDTO.BirthDate,
                    Gender            = dto.EmployeeDTO.Gender,
                    MaritalStatus     = dto.EmployeeDTO.MaritalStatus,
                    // OrganizationLevel = dto.EmployeeDTO.OrganizationLevel, // cast if needed to short?
                    HireDate          = dto.EmployeeDTO.HireDate ?? todayUtcDate,
                };
                db.Employees.Add(employee);
 
                // 6) EmployeeDepartmentHistory
                var history = new EmployeeDepartmentHistory
                {
                    BusinessEntityID = newId,
                    DepartmentID     = dto.DepartmentId,
                    StartDate        = DateTime.UtcNow.Date,
                    EndDate          = null
                };
                db.EmployeeDepartmentHistories.Add(history);
 
                await db.SaveChangesAsync();
                await tx.CommitAsync();
 
                var result = new EmployeeDTO
                {
                    BusinessEntityID  = employee.BusinessEntityID,
                    JobTitle          = employee.JobTitle,
                    NationalIDNumber  = employee.NationalIDNumber,
                    BirthDate         = employee.BirthDate,
                    Gender            = employee.Gender,
                    MaritalStatus     = employee.MaritalStatus,
                    // OrganizationLevel = employee.OrganizationLevel,
                    HireDate          = employee.HireDate,
                };
 
                return CreatedAtAction(nameof(Get), new { id = employee.BusinessEntityID }, result);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to create employee and related records. {ex}");
            }
        }
 
 
 
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, EmployeeDTO employeeDTO)
        {
            if (id != employeeDTO.BusinessEntityID) return BadRequest();
 
            if (employeeDTO.HireDate.HasValue)
            {
                var hireDate = employeeDTO.HireDate.Value;
                if (hireDate <= DateTime.MinValue || hireDate >= DateTime.MaxValue)
                    return BadRequest("HireDate is out of range.");
            }
 
            if (employeeDTO.BirthDate.HasValue)
            {
                var birthDate = employeeDTO.BirthDate.Value;
                if (birthDate <= DateTime.MinValue || birthDate >= DateTime.MaxValue)
                    return BadRequest("BirthDate is out of range.");
            }
 
            var employee = await this.db.Employees.Include(e => e.EmployeeDepartmentHistories).Include(e => e.EmployeePayHistories)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);
 
            if (employee == null) return NotFound();
 
            if (employeeDTO.JobTitle != null) employee.JobTitle = employeeDTO.JobTitle;
            if (employeeDTO.BirthDate.HasValue) employee.BirthDate = employeeDTO.BirthDate.Value;
            if (employeeDTO.Gender != null) employee.Gender = employeeDTO.Gender;
            if (employeeDTO.MaritalStatus != null) employee.MaritalStatus = employeeDTO.MaritalStatus;
            // if (employeeDTO.OrganizationLevel != null) employee.OrganizationLevel = employeeDTO.OrganizationLevel;
            if (employeeDTO.HireDate.HasValue) employee.HireDate = employeeDTO.HireDate.Value;
 
            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<EmployeeDTO>(employee));
        }
    }
}
 