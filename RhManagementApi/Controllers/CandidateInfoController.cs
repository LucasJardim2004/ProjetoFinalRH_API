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
        public async Task<IActionResult> Patch(int id, CandidateInfoDTO candidateDto)
        {
            if (id != candidateDto.JobCandidateID) return BadRequest();

            var candidate = await db.CandidateInfos
                .FirstOrDefaultAsync(e => e.JobCandidateID == id);

            if (candidate == null) return NotFound();

            if (!string.IsNullOrEmpty(candidateDto.JobTitle)) candidate.JobTitle = candidateDto.JobTitle;
            if (!string.IsNullOrEmpty(candidateDto.NationalID)) candidate.NationalID = candidateDto.NationalID;

            // DateTime? => HasValue & Value are valid
            if (candidateDto.BirthDate.HasValue) candidate.BirthDate = candidateDto.BirthDate.Value;

            if (!string.IsNullOrEmpty(candidateDto.MaritalStatus)) candidate.MaritalStatus = candidateDto.MaritalStatus;
            if (!string.IsNullOrEmpty(candidateDto.Gender)) candidate.Gender = candidateDto.Gender;
            if (!string.IsNullOrEmpty(candidateDto.FirstName)) candidate.FirstName = candidateDto.FirstName;
            if (!string.IsNullOrEmpty(candidateDto.MiddleName)) candidate.MiddleName = candidateDto.MiddleName;
            if (!string.IsNullOrEmpty(candidateDto.LastName)) candidate.LastName = candidateDto.LastName;
            if (!string.IsNullOrEmpty(candidateDto.Email)) candidate.Email = candidateDto.Email;
            if (!string.IsNullOrEmpty(candidateDto.Comment)) candidate.Comment = candidateDto.Comment;

            await db.SaveChangesAsync();
            return Ok(mapper.Map<CandidateInfoDTO>(candidate));

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