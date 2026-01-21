using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using RhManagementApi.Constants;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
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
            var openings = await db.Openings.ToListAsync();
            return Ok(openings);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var opening = await db.Openings.FirstOrDefaultAsync(o => o.OpeningID == id);
            if (opening == null) return NotFound();

            var openingDTO = mapper.Map<OpeningDTO>(opening);
            return Ok(openingDTO);
        }

        [HttpPost]
        [Authorize(Policy = "HROnly")]
        public async Task<IActionResult> Create(OpeningDTO openingDTO)
        {
            if (openingDTO.JobTitle == null)
            {
                return BadRequest("Job Title is required");
            }
            openingDTO.DateCreated = DateTime.Now;

            var opening = mapper.Map<Opening>(openingDTO);
            opening.OpenFlag = true;

            db.Openings.Add(opening);
            await db.SaveChangesAsync();

            var readOpeningDTO = mapper.Map<OpeningDTO>(opening);
            return CreatedAtAction(nameof(Get), new { id = opening.OpeningID }, readOpeningDTO);
        }

        [HttpPatch("{id}")]
        [Authorize(Policy = "HROnly")]
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
            if (openingDTO.OpenFlag != opening.OpenFlag) opening.OpenFlag = openingDTO.OpenFlag;

            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<OpeningDTO>(opening));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "HROnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var opening = await db.Openings.FindAsync(id);
            if (opening == null) return NotFound();

            // proactive block if there are related candidates/applications
            var hasCandidates = await db.CandidateInfos.AnyAsync(ci => ci.OpeningID == id);
            if (hasCandidates)
            {
                return Conflict(new
                {
                    code = "OPENING_HAS_CANDIDATES",
                    message = "This opening cannot be deleted because it has associated candidates/applications. Remove the associated records first (or close the opening)."
                });
            }

            db.Openings.Remove(opening);

            try
            {
                await db.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                // safety net for race conditions
                return Conflict(new
                {
                    code = "OPENING_DELETE_CONFLICT",
                    message = "This opening could not be deleted because there are related records (e.g., candidates/applications)."
                });
            }
        }
    }
}