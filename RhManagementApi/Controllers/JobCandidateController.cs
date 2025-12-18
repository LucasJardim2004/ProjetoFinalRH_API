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
        private readonly IWebHostEnvironment env;
        private string ContentRootPath => env.ContentRootPath;
        public JobCandidateController(AdventureWorksContext db, IMapper mapper, IWebHostEnvironment env)
        {
            this.db = db;
            this.mapper = mapper;
            this.env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var candidates = await this.db.JobCandidates
                .ToListAsync();

            return Ok(candidates);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var candidate = await this.db.JobCandidates
                .FirstOrDefaultAsync(o => o.JobCandidateID == id);
            if (candidate == null) return NotFound();

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
            return CreatedAtAction(nameof(Get), new { Id = candidate.JobCandidateID }, readCandidateDTO);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JobCandidate candidateDTO)
        {
            if (id != candidateDTO.JobCandidateID) return BadRequest(); // TODO: VERIFICAR SE ESTA BEM

            var candidate = await this.db.JobCandidates
                .FirstOrDefaultAsync(e => e.JobCandidateID == id);

            if (candidate == null) return NotFound();

            if (candidateDTO.Resume != null) candidate.Resume = candidateDTO.Resume;
            if (candidateDTO.ResumeFile != null) candidate.ResumeFile = candidateDTO.ResumeFile;

            await this.db.SaveChangesAsync();
            return Ok(this.mapper.Map<JobCandidateDTO>(candidate));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var candidate = await this.db.JobCandidates.FindAsync(id);
            if (candidate == null) return NotFound();

            this.db.JobCandidates.Remove(candidate);
            await this.db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("upload-cv")]
        public async Task<IActionResult> UploadCv([FromForm] IFormFile file, [FromForm] string nationalId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (string.IsNullOrWhiteSpace(nationalId))
                return BadRequest("National ID is required.");

            // Pasta onde vamos guardar os CVs (ex: /ProjetoFinalRH_API/CvFiles)
            var uploadsFolder = Path.Combine(ContentRootPath, "CvFiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Extensão original (.pdf, .doc, etc.)
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".pdf"; // fallback
            }

            // Nome final do ficheiro: nationalId + extensão
            var safeNationalId = nationalId.Trim();
            var newFileName = $"{safeNationalId}{extension}".Replace(" ", "_");

            var fullPath = Path.Combine(uploadsFolder, newFileName);

            // Gravar ficheiro no disco
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Devolver só o nome para guardar em ResumeFile
            return Ok(new { fileName = newFileName });
        }

        [HttpGet("download-cv/{fileName}")]
        public IActionResult DownloadCv(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File name is required.");

            var uploadsFolder = Path.Combine(ContentRootPath, "CvFiles");
            var fullPath = Path.Combine(uploadsFolder, fileName);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            const string contentType = "application/octet-stream";
            return PhysicalFile(fullPath, contentType, fileName);
        }
    }
}