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
    public class CandidateInfoController : ControllerBase
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
            var candidates = await this.db.CandidateInfos
                .ToListAsync();

            return Ok(candidates);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get (int id)
        {
            var candidate = await this.db.CandidateInfos
                .FirstOrDefaultAsync(o => o.JobCandidateID == id);
            if (opening == null) return NotFound();

            var candidateDTO = this.mapper.Map<CandidateInfoDTO>(candidate);

            return Ok(candidateDTO);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CandidateInfoDTO candidateDTO)
        {
            var candidate = this.mapper.Map<CandidateInfo>(candidateDTO);
            this.db.CandidateInfos.Add(candidate);
            await this.db.SaveChangesAsync();

            var readCandidateDTO = this.mapper.Map<CandidateInfoDTO>(candidate);
            return CreatedAtAction(nameof(Get),new {Id = candidate.JobCandidateID}, readCandidateDTO);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, CandidateInfoDTO candidateDTO)
        {
            if (id != candidateDTO.JobCandidateID) return BadRequest();

            var candidate = await this.db.CandidateInfos
                .FirstOrDefaultAsync(e => e.JobCandidateID == id);

            if (candidate == null) return NotFound();

            if (candidateDTO.JobTitle != null) candidate.JobTitle = candidateDTO.JobTitle;
            if (candidateDTO.NationalID != null) candidate.NationalID = candidateDTO.NationalID;
            if (candidateDTO.BirthDate.HasValue) candidate.BirthDate = candidateDTO.BirthDate.Value;
            if (candidateDTO.MaritalStatus != null) candidate.MaritalStatus = candidateDTO.MaritalStatus;
            if (candidateDTO.Gender != null) candidate.Gender = candidateDTO.Gender;
            if (candidateDTO.FirstName != null) candidate.FirstName = candidateDTO.FirstName;
            if (candidateDTO.MiddleName != null) candidate.MiddleName = candidateDTO.MiddleName;
            if (candidateDTO.LastName != null) candidate.LastName = candidateDTO.LastName;
            if (candidateDTO.Email != null) candidate.Email = candidateDTO.Email;
            if (candidateDTO.Comment != null) candidate.Comment = candidateDTO.Comment;

            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<candidateDTO>(candidate));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete (int id)
        {
            var candidate = await this.db.CandidateInfos.FindAsync(id);
            if (candidate == null) return NotFound();

            this.db.CandidateInfos.Remove(candidate);
            await this.db.SaveChangesAsync();

            return NoContent();
        }
    }
}