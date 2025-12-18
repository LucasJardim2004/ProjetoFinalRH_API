using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using RhManagementApi.Models;

namespace RhManagementApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EmployeePayHistoryController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;
        public EmployeePayHistoryController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get (int id)
        {
            var employeePayHistory = await this.db.EmployeePayHistories.Where(e => e.BusinessEntityID == id).ToListAsync();
            if (employeePayHistory == null) return NotFound();

            var EmployeePayHistoryDTO = this.mapper.Map<List<EmployeePayHistoryDTO>>(employeePayHistory);

            return Ok(EmployeePayHistoryDTO);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeePayHistoryDTO employeePayHistoryDTO)
        {
            if(employeePayHistoryDTO.RateChangeDate == null)
                employeePayHistoryDTO.RateChangeDate = DateTime.Now;

            var employeePayHistory = this.mapper.Map<EmployeePayHistory>(employeePayHistoryDTO);
            this.db.EmployeePayHistories.Add(employeePayHistory);
            await this.db.SaveChangesAsync();

            var readEmployeePayHistoryDTO = this.mapper.Map<EmployeePayHistoryDTO>(employeePayHistory);
            return CreatedAtAction(nameof(Get),new {Id = employeePayHistory.BusinessEntityID}, readEmployeePayHistoryDTO);
        }
    }
}