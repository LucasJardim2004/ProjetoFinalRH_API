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
    public class PersonPhoneController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;
        public PersonPhoneController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get (int id)
        {
            var phone = await this.db.PeoplePhones
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);
            if (phone == null) return NotFound();

            var phoneDTO = this.mapper.Map<PersonPhoneDTO>(phone);

            return Ok(phoneDTO);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PersonPhoneDTO phoneDTO)
        {
            var phone = this.mapper.Map<PersonPhone>(phoneDTO);
            this.db.PeoplePhones.Add(phone);
            await this.db.SaveChangesAsync();

            var readPhoneDTO = this.mapper.Map<PersonPhoneDTO>(phone);
            return CreatedAtAction(nameof(Get),new {Id = phone.BusinessEntityID}, readPhoneDTO);
        }
    }
}