using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RhManagementApi.Constants;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using AutoMapper;
using RhManagementApi.Models;
 
namespace RhManagementApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EmployeeDepartmentHistoryController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;
        public EmployeeDepartmentHistoryController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }
 
        [HttpGet("{id}")]
        [Authorize(Policy = "EmployeeOrHR")]
        public async Task<IActionResult> Get(int id)
        {
            var histories = await db.EmployeeDepartmentHistories
                .Where(e => e.BusinessEntityID == id)
                .Include(e => e.Department)
                .Select(e => new EmployeeDepartmentHistoryDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    DepartmentID = e.DepartmentID,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    DepartmentName = e.Department.Name 
                })
                .ToListAsync();

            if (!histories.Any())
                return NotFound();

            return Ok(histories);
        }

 
        [HttpPost]
        [Authorize(Policy = "HROnly")]
        public async Task<IActionResult> Create([FromBody] EmployeeDepartmentHistoryDTO dto)
        {
            if (dto == null) return BadRequest("Body is required.");
 
            // Required fields
            if (!dto.BusinessEntityID.HasValue) return BadRequest("BusinessEntityID is required.");
            if (!dto.DepartmentID.HasValue) return BadRequest("DepartmentID is required.");
 
            // Business rules
            if (dto.DepartmentID.Value < 1 || dto.DepartmentID.Value > 16)
                return BadRequest("DepartmentID must be between 1 and 16.");
 
            // Validate EndDate (if provided)
            if (dto.EndDate.HasValue)
            {
                var end = dto.EndDate.Value;
                if (end <= DateTime.MinValue || end >= DateTime.MaxValue)
                    return BadRequest("EndDate is out of range.");
            }
 
            // Choose StartDate (defaults to today if not provided)
            var start = dto.StartDate ?? DateTime.UtcNow.Date;
            if (start <= DateTime.MinValue || start >= DateTime.MaxValue)
                return BadRequest("StartDate is out of range.");
 
            // Map and enforce StartDate/EndDate explicitly
            var entity = new EmployeeDepartmentHistory
            {
                BusinessEntityID = dto.BusinessEntityID.Value,
                DepartmentID     = dto.DepartmentID.Value,
                StartDate        = start,
                EndDate          = dto.EndDate
            };
            entity.StartDate = start;
            if (!dto.EndDate.HasValue) entity.EndDate = null; // Explicitly null is fine
 
            this.db.EmployeeDepartmentHistories.Add(entity);
            await this.db.SaveChangesAsync();
 
            var readDto = this.mapper.Map<EmployeeDepartmentHistoryDTO>(entity);
            return CreatedAtAction(nameof(Get), new { id = entity.BusinessEntityID }, readDto);
        }
       
        [Authorize(Policy = "HROnly")]
        [HttpPatch("{id}_{startDate}")]
        public async Task<IActionResult> Patch(int id, DateTime startDate, EmployeeDepartmentHistoryDTO dto)
        {
            if (id != dto.BusinessEntityID)
                return BadRequest("ID mismatch");

            if (dto.EndDate.HasValue)
            {
                var endDate = dto.EndDate.Value;
                if (endDate <= DateTime.MinValue || endDate >= DateTime.MaxValue)
                    return BadRequest("EndDate is out of range.");
            }

            var target = startDate.Date;

            var edh = await db.EmployeeDepartmentHistories
                .FirstOrDefaultAsync(e =>
                    e.BusinessEntityID == id &&
                    e.StartDate.Date == target);

            if (edh == null)
                return NotFound();

            if (dto.EndDate != null)
                edh.EndDate = dto.EndDate;

            await db.SaveChangesAsync();

            var result = mapper.Map<EmployeeDepartmentHistoryDTO>(edh);
            return Ok(result);
        }
    }
}
 