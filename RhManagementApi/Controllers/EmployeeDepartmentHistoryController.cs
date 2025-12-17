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
        public async Task<IActionResult> Get (int id)
        {
            var employeeDepartmentHistories = await this.db.EmployeeDepartmentHistories.Where(e => e.BusinessEntityID == id).ToListAsync();
            if (employeeDepartmentHistories == null) return NotFound();
 
            var EmployeeDepartmentHistoriesDTO = this.mapper.Map<List<EmployeeDepartmentHistoryDTO>>(employeeDepartmentHistories);
 
            return Ok(EmployeeDepartmentHistoriesDTO);
        }
 
        [HttpPost]
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
            var entity = this.mapper.Map<EmployeeDepartmentHistory>(dto);
            entity.StartDate = start;
            if (!dto.EndDate.HasValue) entity.EndDate = null; // Explicitly null is fine
 
            this.db.EmployeeDepartmentHistories.Add(entity);
            await this.db.SaveChangesAsync();
 
            var readDto = this.mapper.Map<EmployeeDepartmentHistoryDTO>(entity);
            return CreatedAtAction(nameof(Get), new { id = entity.BusinessEntityID }, readDto);
        }
       
        [HttpPatch("{id}_{startDate}")]
        public async Task<IActionResult> Patch(int id, DateTime startDate, EmployeeDepartmentHistoryDTO dto)
        {
            if (id != dto.BusinessEntityID) return BadRequest("ID mismatch");
 
            if (dto.EndDate.HasValue)
            {
                var endDate = dto.EndDate.Value;
                if (endDate <= DateTime.MinValue || endDate >= DateTime.MaxValue)
                    return BadRequest("EndDate is out of range.");
            }
 
            // Find by date-only to avoid TZ/tick mismatches
            var edh = await db.EmployeeDepartmentHistories
                .FirstOrDefaultAsync(e =>
                    e.BusinessEntityID == id &&
                    EF.Functions.DateDiffDay(e.StartDate, startDate) == 0);
 
            if (edh == null) return NotFound();
 
            if (dto.EndDate != null)
                edh.EndDate = dto.EndDate;
 
            await db.SaveChangesAsync();
 
            var result = mapper.Map<EmployeeDepartmentHistoryDTO>(edh);
            return Ok(result);
        }
    }
}
 