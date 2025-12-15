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
    public class JobCandidateController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;
        public JobCandidateController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var candidates = await this.db.JobCandidates
                .ToListAsync();

            return Ok(candidates);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get (int id)
        {
            var candidate = await this.db.JobCandidates
                .FirstOrDefaultAsync(o => o.JobCandidateID == id);
            if (opening == null) return NotFound();

            var candidateDTO = this.mapper.Map<JobCandidateDTO>(candidate);

            return Ok(candidateDTO);
        }

        [HttpPost]
        public async Task<IActionResult> Create(JobCandidateDTO candidateDTO)
        {
            var candidate = this.mapper.Map<JobCandidate>(candidateDTO);
            this.db.JobCandidates.Add(candidate);
            await this.db.SaveChangesAsync();

            var readCandidateDTO = this.mapper.Map<JobCandidateDTO>(candidate);
            return CreatedAtAction(nameof(Get),new {Id = candidate.JobCandidateID}, readCandidateDTO);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JobCandidate candidateDTO)
        {
            if (id != candidateDTO.JobCandidateID) return BadRequest();

            var candidate = await this.db.JobCandidates
                .FirstOrDefaultAsync(e => e.JobCandidateID == id);

            if (candidate == null) return NotFound();

            if (candidateDTO.Resume != null) candidate.Resume = candidateDTO.Resume;
            if (candidateDTO.ResumeFile != null) candidate.ResumeFile = candidateDTO.ResumeFile;

            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<JobCandidateDTO>(candidate));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete (int id)
        {
            var candidate = await this.db.JobCandidates.FindAsync(id);
            if (candidate == null) return NotFound();

            this.db.CandidateInfos.Remove(candidate);
            await this.db.SaveChangesAsync();

            return NoContent();
        }
    }
}