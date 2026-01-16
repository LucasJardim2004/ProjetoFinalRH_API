using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RhManagementApi.Constants;
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
        private readonly IWebHostEnvironment env;
        private string ContentRootPath => env.ContentRootPath;
        public JobCandidateController(AdventureWorksContext db, IMapper mapper, IWebHostEnvironment env)
        {
            this.db = db;
            this.mapper = mapper;
            this.env = env;
        }

        [HttpGet]
        [Authorize(Policy = "HROnly")]
        public async Task<IActionResult> GetAll()
        {
            var candidates = await this.db.JobCandidates
                .ToListAsync();

            return Ok(candidates);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "HROnly")]
        public async Task<IActionResult> Get(int id)
        {
            var candidate = await this.db.JobCandidates
                .FirstOrDefaultAsync(o => o.JobCandidateID == id);
            if (candidate == null) return NotFound();

            var candidateDTO = this.mapper.Map<JobCandidateDTO>(candidate);

            return Ok(candidateDTO);
        }

        [Authorize(Policy = "HROnly")]
        [HttpPost]
        public async Task<IActionResult> Create(JobCandidateDTO candidateDTO)
        {
            var candidate = this.mapper.Map<JobCandidate>(candidateDTO);
            this.db.JobCandidates.Add(candidate);
            await this.db.SaveChangesAsync();

            var readCandidateDTO = this.mapper.Map<JobCandidateDTO>(candidate);
            return CreatedAtAction(nameof(Get), new { Id = candidate.JobCandidateID }, readCandidateDTO);
        }

        [HttpPatch("{id}")]
        [Authorize(Policy = "HROnly")]
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
        [Authorize(Policy = "HROnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var candidate = await this.db.JobCandidates.FindAsync(id);
            if (candidate == null) return NotFound();

            this.db.JobCandidates.Remove(candidate);
            await this.db.SaveChangesAsync();

            return NoContent();
        }

        [Authorize(Policy = "HROnly")]
        [HttpPost("upload-cv/{jobCandidateID}")]
        public async Task<IActionResult> UploadCv(int jobCandidateID, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Extensão original (.pdf, .doc, etc.)
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".pdf";
            }

            // Naming convention: originalfilename_currentdate_newguid
            var originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
            var cleanFileName = originalFileNameWithoutExtension
                .Replace(" ", "_")
                .ToLower();
            var currentDate = DateTime.Now.ToString("yyyyMMdd");
            var newGuid = Guid.NewGuid().ToString();
            var newFileName = $"{cleanFileName}_{currentDate}_{newGuid}{extension}";

            // Read file bytes
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Find JobCandidate and update CVFile and CVFileName
                var jobCandidate = await this.db.JobCandidates.FindAsync(jobCandidateID);
                if (jobCandidate == null)
                    return NotFound("Job candidate not found.");

                jobCandidate.CVFile = fileBytes;
                jobCandidate.CVFileName = newFileName;

                await this.db.SaveChangesAsync();
            }

            // // Devolver só o nome para guardar em ResumeFile
            return Ok(new { fileName = newFileName });
        }

        // [HttpGet("download-cv/{fileName}")]
        // public IActionResult DownloadCv(string fileName)
        // {
        //     if (string.IsNullOrWhiteSpace(fileName))
        //         return BadRequest("File name is required.");

        //     var uploadsFolder = Path.Combine(ContentRootPath, "CvFiles");
        //     var fullPath = Path.Combine(uploadsFolder, fileName);

        //     if (!System.IO.File.Exists(fullPath))
        //         return NotFound();

        //     const string contentType = "application/octet-stream";
        //     return PhysicalFile(fullPath, contentType, fileName);
        // }
    }
}