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
    public class OpeningController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;
        public OpeningController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }
 
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var openings = await this.db.Openings
                .ToListAsync();
 
            return Ok(openings);
        }
 
        [HttpGet("{id}")]
        public async Task<IActionResult> Get (int id)
        {
            var opening = await this.db.Openings
                .FirstOrDefaultAsync(o => o.OpeningID == id);
            if (opening == null) return NotFound();
 
            var openingDTO = this.mapper.Map<OpeningDTO>(opening);
 
            return Ok(openingDTO);
        }
 
        [HttpPost]
        public async Task<IActionResult> Create(OpeningDTO openingDTO)
        {
            var dateCreated = openingDTO.DateCreated.Value;
            if (openingDTO.DateCreated.HasValue)
            {
               
                if (dateCreated < DateTime.MinValue || dateCreated > DateTime.MaxValue)
                    return BadRequest("StartDate is out of range.");
            }
            else {dateCreated = DateTime.Now;}
 
            var opening = this.mapper.Map<Opening>(openingDTO);
 
            opening.OpenFlag = false;
 
            this.db.Openings.Add(opening);
            await this.db.SaveChangesAsync();
 
            var readOpeningDTO = this.mapper.Map<OpeningDTO>(opening);
            return CreatedAtAction(nameof(Get),new {Id = opening.OpeningID}, readOpeningDTO);
        }
 
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, OpeningDTO openingDTO)
        {
            if (id != openingDTO.OpeningID) return BadRequest();
 
            if (openingDTO.DateCreated.HasValue)
            {
                var dateCreated = openingDTO.DateCreated.Value;
                if (dateCreated <= DateTime.MinValue || dateCreated >= DateTime.MaxValue)
                    return BadRequest("StartDate is out of range.");
            }
 
            var opening = await this.db.Openings
                .FirstOrDefaultAsync(e => e.OpeningID == id);
 
            if (opening == null) return NotFound();
 
            if (openingDTO.JobTitle != null) opening.JobTitle = openingDTO.JobTitle;
            if (openingDTO.Description != null) opening.Description = openingDTO.Description;
            // if (openingDTO.DateCreated != null) opening.DateCreated = openingDTO.DateCreated; TODO: VERIFICAR SE E POSSIVEL MUDAR
            if (openingDTO.OpenFlag != null) opening.OpenFlag = openingDTO.OpenFlag; // TODO: VERIFICAR CONDICAO QUE E SEMPRE TRUE
 
            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<OpeningDTO>(opening));
        }
 
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete (int id)
        {
            var opening = await this.db.Openings.FindAsync(id);
            if (opening == null) return NotFound();
 
            this.db.Openings.Remove(opening);
            await this.db.SaveChangesAsync();
 
            return NoContent();
        }
    }
}
 