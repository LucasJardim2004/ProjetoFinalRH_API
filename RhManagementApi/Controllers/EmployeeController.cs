using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Constants;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using AutoMapper;
using RhManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RhManagementApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly AuthDbContext authDb;
        private readonly IMapper mapper;
        public EmployeeController(AdventureWorksContext db, AuthDbContext authDb, IMapper mapper)
        {
            this.db = db;
            this.authDb = authDb;
            this.mapper = mapper;
        }

        [HttpGet]
        //[Authorize(Policy = "HROnly")]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string? searchTerm = null)
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit max page size for security

            // Build query with search filter
            var query = this.db.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                query = query.Where(e => 
                    e.JobTitle.ToLower().Contains(search) ||
                    e.NationalIDNumber.ToLower().Contains(search)
                );
            }

            // Get total count for pagination metadata
            var totalCount = await query.CountAsync();

            // Get paginated employees
            var employees = await query
                .OrderBy(e => e.BusinessEntityID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map employees to DTOs
            var employeeDtos = this.mapper.Map<List<EmployeeDTO>>(employees);
 
            return Ok(new
            {
                data = employeeDtos,
                pagination = new
                {
                    totalCount = totalCount,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
 
 
        [HttpGet("{id}")]
        [Authorize(Policy = "EmployeeOrHR")]
        public async Task<IActionResult> Get(int id)
        {
            // Employees can only view their own info; HR can view anyone
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userBeId = User.FindFirst("business_entity_id")?.Value;

            if (userRole == RoleNames.Employee && int.TryParse(userBeId, out var currentUserBeId) && currentUserBeId != id)
                return Forbid(); // Employee trying to view someone else's data

            // 1) Load Employee with its own relationships
            var employee = await db.Employees
                .Include(e => e.EmployeeDepartmentHistories)
                    .ThenInclude(h => h.Department)
                .Include(e => e.EmployeePayHistories)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);
 
            if (employee == null) return NotFound();
 
            var phoneNumber = await db.PeoplePhones
                .Where(ph => ph.BusinessEntityID == id)
                .Select(ph => ph.PhoneNumber)
                .FirstOrDefaultAsync();
 
            var emailAddress = await db.EmailAddresses
                .Where(em => em.BusinessEntityID == id)
                .Select(em => em.EmailAddress)
                .FirstOrDefaultAsync();

            
            var person = await db.People
                .Where(p => p.BusinessEntityID == id)
                .Select(p => new {p.FirstName, p.LastName})
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
                EmailAddress = emailAddress,
                FirstName = person?.FirstName,
                LastName = person?.LastName
            });
        }

        [HttpGet("role/hr")]
        // [Authorize(Policy = "EmployeeOrHR")]
        public async Task<IActionResult> GetHR()
        {
            // Get all BusinessEntityIDs of users with HR role
            var hrEmployeeIds = await authDb.UserRoles
                .Join(
                    authDb.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, r.Name }
                )
                .Where(x => x.Name == RoleNames.HR)
                .Join(
                    authDb.Users,
                    x => x.UserId,
                    u => u.Id,
                    (x, u) => u.BusinessEntityID
                )
                .Where(beId => beId.HasValue)
                .Select(beId => beId.Value)
                .ToListAsync();

            return Ok(hrEmployeeIds);
        }
 


[HttpPost]
[Authorize(Policy = "HROnly")]
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

    var today = DateTime.UtcNow.Date;

    try
    {
        //
        // 1) Create BusinessEntity (EF CAN handle this)
        //
        var be = new BusinessEntity();
        db.BusinessEntities.Add(be);
        await db.SaveChangesAsync();

        int newId = be.BusinessEntityID;

        //
        // 2) Insert the Person row via RAW SQL (EF CANNOT insert Person due to triggers)
        //
        await db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO [Person].[Person]
                ([BusinessEntityID], [PersonType], [NameStyle], [FirstName], [LastName],
                 [EmailPromotion], [rowguid], [ModifiedDate])
            VALUES
                ({0}, {1}, 0, {2}, {3}, 0, NEWID(), GETDATE());
        ",
        newId,
        dto.PersonType.Trim(),
        dto.FirstName.Trim(),
        dto.LastName.Trim()
        );

        //
        // 3) Now EF CAN insert into dependent tables safely
        //
        db.EmailAddresses.Add(new PersonEmailAddress
        {
            BusinessEntityID = newId,
            EmailAddress      = dto.EmailAddress.Trim()
        });

        db.PeoplePhones.Add(new PersonPhone
        {
            BusinessEntityID    = newId,
            PhoneNumber         = dto.PhoneNumber.Trim(),
            PhoneNumberTypeID   = 1
        });

        db.Employees.Add(new Employee
        {
            BusinessEntityID  = newId,
            JobTitle          = dto.EmployeeDTO.JobTitle,
            NationalIDNumber  = dto.EmployeeDTO.NationalIDNumber,
            BirthDate         = dto.EmployeeDTO.BirthDate,
            Gender            = dto.EmployeeDTO.Gender,
            MaritalStatus     = dto.EmployeeDTO.MaritalStatus,
            HireDate          = dto.EmployeeDTO.HireDate ?? today
        });

        db.EmployeeDepartmentHistories.Add(new EmployeeDepartmentHistory
        {
            BusinessEntityID = newId,
            DepartmentID     = dto.DepartmentId,
            StartDate        = today,
            EndDate          = null
        });

        //
        // 4) Commit remaining inserts
        //
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = newId }, new { BusinessEntityID = newId });
    }
    catch (Exception ex)
    {
        return StatusCode(500, "Failed to create employee and related records. " + ex);
    }
}



        [HttpPatch("{id}")]
        [Authorize(Policy = "EmployeeOrHR")]
        public async Task<IActionResult> Patch(int id, EmployeeDTO employeeDTO)
        {
            // Employees can only modify their own data; HR can modify anyone (but with restrictions)
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userBeId = User.FindFirst("business_entity_id")?.Value;

            if (userRole == RoleNames.Employee && int.TryParse(userBeId, out var currentUserBeId) && currentUserBeId != id)
                return Forbid(); // Employee trying to modify someone else's data
            
            if (id != employeeDTO.BusinessEntityID) return BadRequest();

            // Validate date ranges if provided
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
 
            // Determine if HR is modifying their own profile
            bool isHRModifyingOwnProfile = userRole == RoleNames.HR && int.TryParse(userBeId, out var hrBeId) && hrBeId == id;
 
            // Both Employee and HR modifying own profile can only change Gender and Marital Status
            // HR modifying someone else can change all these fields
            if (employeeDTO.Gender != null) employee.Gender = employeeDTO.Gender;
            if (employeeDTO.MaritalStatus != null) employee.MaritalStatus = employeeDTO.MaritalStatus;

            // Only HR can change these fields, and only if modifying someone else's profile
            if (userRole == RoleNames.HR && !isHRModifyingOwnProfile)
            {
                if (employeeDTO.JobTitle != null) employee.JobTitle = employeeDTO.JobTitle;
                if (employeeDTO.BirthDate.HasValue) employee.BirthDate = employeeDTO.BirthDate.Value;
                if (employeeDTO.HireDate.HasValue) employee.HireDate = employeeDTO.HireDate.Value;
                // if (employeeDTO.OrganizationLevel != null) employee.OrganizationLevel = employeeDTO.OrganizationLevel;
            }
 
            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<EmployeeDTO>(employee));
        }
    }
}
 