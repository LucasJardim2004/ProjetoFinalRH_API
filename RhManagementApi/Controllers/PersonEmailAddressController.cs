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
    public class PersonEmailAddressController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;
        public PersonEmailAddressController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "EmployeeOrHR")]
        public async Task<IActionResult> Get (int id)
        {
            var emailAddress = await this.db.EmailAddresses
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);
            if (emailAddress == null) return NotFound();

            var emailAddressDTO = this.mapper.Map<PersonEmailAddressDTO>(emailAddress);

            return Ok(emailAddressDTO);
        }

        [Authorize(Policy = "EmployeeOrHR")]
        [HttpPost]
        public async Task<IActionResult> Create(PersonEmailAddressDTO emailAddressDTO)
        {
            if (emailAddressDTO.EmailAddress == null)
            {
                return BadRequest("Email Address is required");
            }
            var emailAddress = this.mapper.Map<PersonEmailAddress>(emailAddressDTO);
            this.db.EmailAddresses.Add(emailAddress);
            await this.db.SaveChangesAsync();

            var readEmailAddressDTO = this.mapper.Map<PersonEmailAddressDTO>(emailAddress);
            return CreatedAtAction(nameof(Get),new {Id = emailAddress.BusinessEntityID}, readEmailAddressDTO);
        }

        [Authorize(Policy = "EmployeeOrHR")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, PersonEmailAddressDTO emailAddressDTO)
        {
            if (id != emailAddressDTO.BusinessEntityID) return BadRequest();

            var emailAddress = await this.db.EmailAddresses
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

            if (emailAddress == null) return NotFound();

            if (emailAddressDTO.EmailAddress != null) emailAddress.EmailAddress = emailAddressDTO.EmailAddress;

            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<PersonEmailAddressDTO>(emailAddress));
        }
    }
}