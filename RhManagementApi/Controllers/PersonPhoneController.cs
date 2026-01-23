using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RhManagementApi.Constants;
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
        [Authorize(Policy = "EmployeeOrHR")]
        public async Task<IActionResult> Get (int id)
        {
            var phone = await this.db.PeoplePhones
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);
            if (phone == null) return NotFound();

            var phoneDTO = this.mapper.Map<PersonPhoneDTO>(phone);

            return Ok(phoneDTO);
        }

        [HttpPost]
        [Authorize(Policy = "EmployeeOrHR")]
        public async Task<IActionResult> Create(PersonPhoneDTO phoneDTO)
        {
            if (phoneDTO.BusinessEntityID == null)
            {
                return BadRequest("BusinessEntityID is required");
            }

            if (phoneDTO.PhoneNumber == null)
            {
                return BadRequest("Phone Number is required");
            }
            var phone = this.mapper.Map<PersonPhone>(phoneDTO);
            this.db.PeoplePhones.Add(phone);
            await this.db.SaveChangesAsync();

            var readPhoneDTO = this.mapper.Map<PersonPhoneDTO>(phone);
            return CreatedAtAction(nameof(Get),new {Id = phone.BusinessEntityID}, readPhoneDTO);
        }

        [HttpDelete("{businessEntityId}/{phoneNumber}/{phoneNumberTypeId}")]
        [Authorize(Policy = "EmployeeOrHR")]
        public async Task<IActionResult> DeletePhone(int businessEntityId, string phoneNumber, int phoneNumberTypeId)
        {
            var phone = await this.db.PeoplePhones
                .FirstOrDefaultAsync(e => e.BusinessEntityID == businessEntityId && 
                                          e.PhoneNumber == phoneNumber && 
                                          e.PhoneNumberTypeID == phoneNumberTypeId);
            
            if (phone == null) return NotFound();

            this.db.PeoplePhones.Remove(phone);
            await this.db.SaveChangesAsync();

            return NoContent();
        }
    }
}