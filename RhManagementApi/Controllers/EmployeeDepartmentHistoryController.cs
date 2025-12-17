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
        public async Task<IActionResult> Create(EmployeeDepartmentHistoryDTO employeeDepartmentHistoryDTO)
        {
            if (employeeDepartmentHistoryDTO.EndDate.HasValue)
            {
                var endDate = employeeDepartmentHistoryDTO.EndDate.Value;
                if (endDate <= DateTime.MinValue || endDate >= DateTime.MaxValue)
                    return BadRequest("EndDate is out of range.");
            }
            var startDate = employeeDepartmentHistoryDTO.StartDate.Value;
            if (employeeDepartmentHistoryDTO.StartDate.HasValue)
            {
               
                if (startDate <= DateTime.MinValue || startDate >= DateTime.MaxValue)
                    return BadRequest("StartDate is out of range.");
            }
            else {startDate = DateTime.Now;}
 
            var employeeDepartmentHistory = this.mapper.Map<EmployeeDepartmentHistory>(employeeDepartmentHistoryDTO);
            this.db.EmployeeDepartmentHistories.Add(employeeDepartmentHistory);
            await this.db.SaveChangesAsync();
 
            var readEmployeeDepartmentHistoryDTO = this.mapper.Map<EmployeeDepartmentHistoryDTO>(employeeDepartmentHistory);
            return CreatedAtAction(nameof(Get),new {Id = employeeDepartmentHistory.BusinessEntityID}, readEmployeeDepartmentHistoryDTO);
        }
 
        // TODO: TORNAR BONITO
        [HttpPatch("{id}_{startDate}")]
        public async Task<IActionResult> Patch(int id, DateTime startDate, EmployeeDepartmentHistoryDTO employeeDepartmentHistoryDTO)
        {
            if (id != employeeDepartmentHistoryDTO.BusinessEntityID) return BadRequest();
 
            if (employeeDepartmentHistoryDTO.EndDate.HasValue)
            {
                var endDate = employeeDepartmentHistoryDTO.EndDate.Value;
                if (endDate <= DateTime.MinValue || endDate >= DateTime.MaxValue)
                    return BadRequest("EndDate is out of range.");
            }
 
            if (employeeDepartmentHistoryDTO.StartDate.HasValue)
            {
                var parameterStartDate = employeeDepartmentHistoryDTO.StartDate.Value;
                if (parameterStartDate <= DateTime.MinValue || parameterStartDate >= DateTime.MaxValue)
                    return BadRequest("StartDate is out of range.");
            }
 
            var employeeDepartmentHistory = await this.db.EmployeeDepartmentHistories
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id && e.StartDate == startDate);
 
            if (employeeDepartmentHistory == null) return NotFound();
 
            if (employeeDepartmentHistoryDTO.EndDate != null) employeeDepartmentHistory.EndDate = employeeDepartmentHistoryDTO.EndDate;
 
            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<EmployeeDepartmentHistoryDTO>(employeeDepartmentHistory));
        }
    }
}
 