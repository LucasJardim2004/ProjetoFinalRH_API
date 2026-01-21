using Xunit;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Controllers;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using RhManagementApi.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

public class JobCandidateControllerTests
{
    private AdventureWorksContext BuildContext()
    {
        var opts = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AdventureWorksContext(opts);
    }

    private static IMapper FakeMapper() => A.Fake<IMapper>();
    private static IWebHostEnvironment FakeEnv()
    {
        var env = A.Fake<IWebHostEnvironment>();
        A.CallTo(() => env.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        return env;
    }

    private static JobCandidate MakeCandidate(int id)
    {
        return new JobCandidate
        {
            JobCandidateID = id,
            Resume = "InitialResume",
            ResumeFile = "initial.pdf",
            CVFile = null,
            CVFileName = null
        };
    }

    // ------------------------------------------------------------------
    // GET ALL
    // ------------------------------------------------------------------
    [Fact]
    public async Task GetAll_ReturnsList()
    {
        await using var db = BuildContext();

        db.JobCandidates.Add(MakeCandidate(1));
        db.JobCandidates.Add(MakeCandidate(2));
        await db.SaveChangesAsync();

        var mapper = FakeMapper();
        var env = FakeEnv();
        var controller = new JobCandidateController(db, mapper, env);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<List<JobCandidate>>(ok.Value);

        Assert.Equal(2, list.Count);
    }

    // ------------------------------------------------------------------
    // GET BY ID
    // ------------------------------------------------------------------
    [Fact]
    public async Task Get_ReturnsOk_WhenFound()
    {
        await using var db = BuildContext();

        var jc = MakeCandidate(10);
        db.JobCandidates.Add(jc);
        await db.SaveChangesAsync();

        var mapper = FakeMapper();
        A.CallTo(() => mapper.Map<JobCandidateDTO>(jc))
            .Returns(new JobCandidateDTO { JobCandidateID = 10, Resume = "InitialResume" });

        var env = FakeEnv();
        var controller = new JobCandidateController(db, mapper, env);

        var result = await controller.Get(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<JobCandidateDTO>(ok.Value);
        Assert.Equal(10, dto.JobCandidateID);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenMissing()
    {
        await using var db = BuildContext();

        var mapper = FakeMapper();
        var env = FakeEnv();

        var controller = new JobCandidateController(db, mapper, env);

        var result = await controller.Get(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ------------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------------
    [Fact]
    public async Task Create_ReturnsCreated()
    {
        await using var db = BuildContext();

        var mapper = FakeMapper();
        var env = FakeEnv();

        var inputDto = new JobCandidateDTO
        {
            JobCandidateID = 20,
            Resume = "R1",
            ResumeFile = "resume.xml"
        };

        var mappedEntity = new JobCandidate
        {
            JobCandidateID = 20,
            Resume = "R1",
            ResumeFile = "resume.xml"
        };

        A.CallTo(() => mapper.Map<JobCandidate>(inputDto))
            .Returns(mappedEntity);

        A.CallTo(() => mapper.Map<JobCandidateDTO>(mappedEntity))
            .Returns(inputDto);

        var controller = new JobCandidateController(db, mapper, env);

        var result = await controller.Create(inputDto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<JobCandidateDTO>(created.Value);

        Assert.Equal(20, dto.JobCandidateID);
        Assert.Single(db.JobCandidates);
    }

    // ------------------------------------------------------------------
    // PATCH
    // ------------------------------------------------------------------
    [Fact]
    public async Task Patch_ReturnsOk_WhenValid()
    {
        await using var db = BuildContext();

        var existing = MakeCandidate(5);
        existing.Resume = "OldResume";
        existing.ResumeFile = "old.pdf";

        db.JobCandidates.Add(existing);
        await db.SaveChangesAsync();

        var mapper = FakeMapper();
        A.CallTo(() => mapper.Map<JobCandidateDTO>(existing))
            .Returns(new JobCandidateDTO
            {
                JobCandidateID = 5,
                Resume = "NewResume",
                ResumeFile = "new.pdf"
            });

        var env = FakeEnv();
        var controller = new JobCandidateController(db, mapper, env);

        var patchInput = new JobCandidate
        {
            JobCandidateID = 5,
            Resume = "NewResume",
            ResumeFile = "new.pdf"
        };

        var result = await controller.Patch(5, patchInput);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<JobCandidateDTO>(ok.Value);

        Assert.Equal("NewResume", existing.Resume);
        Assert.Equal("new.pdf", existing.ResumeFile);
        Assert.Equal(5, dto.JobCandidateID);
    }

    [Fact]
    public async Task Patch_ReturnsBadRequest_WhenIdMismatch()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();
        var env = FakeEnv();

        var controller = new JobCandidateController(db, mapper, env);

        var patchInput = new JobCandidate
        {
            JobCandidateID = 99
        };

        var result = await controller.Patch(5, patchInput);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Patch_ReturnsNotFound_WhenMissing()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();
        var env = FakeEnv();

        var controller = new JobCandidateController(db, mapper, env);

        var patchInput = new JobCandidate { JobCandidateID = 50 };

        var result = await controller.Patch(50, patchInput);

        Assert.IsType<NotFoundResult>(result);
    }

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------
    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        await using var db = BuildContext();

        db.JobCandidates.Add(MakeCandidate(7));
        await db.SaveChangesAsync();

        var mapper = FakeMapper();
        var env = FakeEnv();

        var controller = new JobCandidateController(db, mapper, env);

        var result = await controller.Delete(7);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.JobCandidates);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();
        var env = FakeEnv();

        var controller = new JobCandidateController(db, mapper, env);

        var result = await controller.Delete(123);

        Assert.IsType<NotFoundResult>(result);
    }

    // ------------------------------------------------------------------
    // UPLOAD CV
    // ------------------------------------------------------------------

    [Fact]
    public async Task UploadCv_ReturnsOk_WhenValid()
    {
        await using var db = BuildContext();

        var jc = MakeCandidate(50);
        db.JobCandidates.Add(jc);
        await db.SaveChangesAsync();

        var mapper = FakeMapper();
        var env = FakeEnv();
        var controller = new JobCandidateController(db, mapper, env);

        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var stream = new MemoryStream(fileBytes);
        var file = A.Fake<IFormFile>();
        A.CallTo(() => file.Length).Returns(fileBytes.Length);
        A.CallTo(() => file.FileName).Returns("resume.pdf");
        A.CallTo(() => file.CopyToAsync(A<Stream>._, default))
            .Invokes((Stream target, System.Threading.CancellationToken _) => stream.CopyToAsync(target));

        var result = await controller.UploadCv(50, file);

        var ok = Assert.IsType<OkObjectResult>(result);

        var value = ok.Value!;
        var prop = value.GetType().GetProperty("fileName");
        Assert.NotNull(prop);

        var fileName = prop!.GetValue(value) as string;
        Assert.False(string.IsNullOrWhiteSpace(fileName));

        var updated = await db.JobCandidates.FindAsync(50);

        Assert.NotNull(updated.CVFile);
        Assert.Equal(fileBytes, updated.CVFile);
    }


    [Fact]
    public async Task UploadCv_ReturnsBadRequest_WhenEmptyFile()
    {
        await using var db = BuildContext();

        var mapper = FakeMapper();
        var env = FakeEnv();
        var controller = new JobCandidateController(db, mapper, env);

        var file = A.Fake<IFormFile>();
        A.CallTo(() => file.Length).Returns(0);

        var result = await controller.UploadCv(5, file);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded.", bad.Value);
    }

    [Fact]
    public async Task UploadCv_ReturnsNotFound_WhenCandidateMissing()
    {
        await using var db = BuildContext();

        var mapper = FakeMapper();
        var env = FakeEnv();
        var controller = new JobCandidateController(db, mapper, env);

        var file = A.Fake<IFormFile>();
        A.CallTo(() => file.Length).Returns(10);
        A.CallTo(() => file.FileName).Returns("resume.pdf");

        var result = await controller.UploadCv(999, file);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Job candidate not found.", notFound.Value);
    }
}
