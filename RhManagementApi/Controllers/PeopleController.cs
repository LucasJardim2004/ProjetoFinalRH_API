using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Data;

namespace RhManagementApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PeopleController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        public PeopleController(AdventureWorksContext db)
        {
            this.db = db;
        }

        [HttpGet("/1")]
        public async Task<IActionResult> GetAll()
        {
            var employees = await this.db.CandidateInfos
                .Take(5).ToListAsync();

            return Ok(employees);
        }
        [HttpGet("/2")]
        public async Task<IActionResult> GetAll2()
        {
            var employees = await this.db.Departments
                .Take(5).ToListAsync();

            return Ok(employees);
        }
        [HttpGet("/3")]
        public async Task<IActionResult> GetAll3()
        {
            var employees = await this.db.Employees
                .Take(5).ToListAsync();

            return Ok(employees);
        }
        [HttpGet("/4")]
        public async Task<IActionResult> GetAll4()
        {
            var employees = await this.db.EmployeeDepartmentHistories
                .Take(5).ToListAsync();

            return Ok(employees);
        }
        [HttpGet("/5")]
        public async Task<IActionResult> GetAll5()
        {
            var employees = await this.db.EmployeePayHistories
                .Take(5).ToListAsync();

            return Ok(employees);
        }
        [HttpGet("/6")]
        public async Task<IActionResult> GetAll6()
        {
            var employees = await this.db.JobCandidates
                .Take(5).ToListAsync();

            return Ok(employees);
        }
        [HttpGet("/7")]
        public async Task<IActionResult> GetAll7()
        {
            var employees = await this.db.Logins
                .Take(5).ToListAsync();

            return Ok(employees);
        }
        [HttpGet("/8")]
        public async Task<IActionResult> GetAll8()
        {
            var employees = await this.db.Openings
                .Take(5).ToListAsync();

            return Ok(employees);
        }
        [HttpGet("/9")]
        public async Task<IActionResult> GetAll9()
        {
            var employees = await this.db.People
                .Take(5).ToListAsync();

            return Ok(employees);
        }
    }
}

