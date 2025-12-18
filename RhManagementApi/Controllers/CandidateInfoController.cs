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
        public CandidateInfoController(AdventureWorksContext db, IMapper mapper)
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
            if (candidate == null) return NotFound();
 
            var candidateDTO = this.mapper.Map<CandidateInfoDTO>(candidate);
 
            return Ok(candidateDTO);
        }

        [HttpGet("by-opening/{openingId}")]
        public async Task<IActionResult> GetByOpening(int openingId)
        {
        var candidates = await this.db.CandidateInfos
        .Where(c => c.OpeningID == openingId)
        .Include(c => c.Opening)
        .Include(c => c.JobCandidate)
        .ToListAsync();

        var candidateDtos = this.mapper.Map<List<CandidateInfoDTO>>(candidates);

        return Ok(candidateDtos);
        }

 
        [HttpPost]
        public async Task<IActionResult> Create(CandidateInfoDTO candidateDTO)
        {
            if (candidateDTO.BirthDate.HasValue)
            {
                var birthDate = candidateDTO.BirthDate.Value;
                if (birthDate <= DateTime.MinValue || birthDate >= DateTime.MaxValue)
                    return BadRequest("BirthDate is out of range.");
            }
 
            var candidate = this.mapper.Map<CandidateInfo>(candidateDTO);

            var jobCandidate = await this.db.JobCandidates.FirstOrDefaultAsync(j => j.JobCandidateID == candidate.JobCandidateID);
            
            candidate.JobCandidate = jobCandidate;
            
            this.db.CandidateInfos.Add(candidate);
            await this.db.SaveChangesAsync();
 
            var readCandidateDTO = this.mapper.Map<CandidateInfoDTO>(candidate);
            return CreatedAtAction(nameof(Get), new { id = candidate.JobCandidateID }, readCandidateDTO);
        }
 
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, CandidateInfoDTO candidateDTO)
        {
            if (id != candidateDTO.JobCandidateID) return BadRequest();
 
            if (candidateDTO.BirthDate.HasValue)
            {
                var birthDate = candidateDTO.BirthDate.Value;
                if (birthDate <= DateTime.MinValue || birthDate >= DateTime.MaxValue)
                    return BadRequest("BirthDate is out of range.");
            }
 
            var candidate = await db.CandidateInfos
                .FirstOrDefaultAsync(e => e.JobCandidateID == id);
 
            if (candidate == null) return NotFound();
 
            if (!string.IsNullOrEmpty(candidateDTO.JobTitle))       candidate.JobTitle       = candidateDTO.JobTitle;
            if (!string.IsNullOrEmpty(candidateDTO.NationalID))     candidate.NationalID     = candidateDTO.NationalID;
            if (candidateDTO.BirthDate.HasValue)                    candidate.BirthDate      = candidateDTO.BirthDate.Value;
            if (!string.IsNullOrEmpty(candidateDTO.MaritalStatus))  candidate.MaritalStatus  = candidateDTO.MaritalStatus;
            if (!string.IsNullOrEmpty(candidateDTO.Gender))         candidate.Gender         = candidateDTO.Gender;
            if (!string.IsNullOrEmpty(candidateDTO.FirstName))      candidate.FirstName      = candidateDTO.FirstName;
            if (!string.IsNullOrEmpty(candidateDTO.MiddleName))     candidate.MiddleName     = candidateDTO.MiddleName;
            if (!string.IsNullOrEmpty(candidateDTO.LastName))       candidate.LastName       = candidateDTO.LastName;
            if (!string.IsNullOrEmpty(candidateDTO.Email))          candidate.Email          = candidateDTO.Email;
            if (!string.IsNullOrEmpty(candidateDTO.Comment))        candidate.Comment        = candidateDTO.Comment;
 
            await db.SaveChangesAsync();
            return Ok(this.mapper.Map<CandidateInfoDTO>(candidate));
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
 