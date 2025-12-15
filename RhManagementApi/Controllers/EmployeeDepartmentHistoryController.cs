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

            var employeeDepartmentHistory = await this.db.EmployeeDepartmentHistories
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id && e.StartDate == startDate);

            if (employeeDepartmentHistory == null) return NotFound();

            if (employeeDepartmentHistoryDTO.EndDate != null) employeeDepartmentHistory.EndDate = employeeDepartmentHistoryDTO.EndDate;

            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<EmployeeDepartmentHistoryDTO>(employeeDepartmentHistory));
        }

    }
}