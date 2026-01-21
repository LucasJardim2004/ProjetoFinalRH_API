
using Xunit;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Controllers;
using RhManagementApi.Data;
using RhManagementApi.Models;
using RhManagementApi.DTOs;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class CandidateInfoControllerTests
{
    private AdventureWorksContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AdventureWorksContext(options);
    }

    private static IMapper FakeMapper() => A.Fake<IMapper>();

    /// <summary>
    /// Builds a minimal, EF-valid CandidateInfo with all required fields populated.
    /// Adjust defaults if your model requires different constraints.
    /// </summary>
    private static CandidateInfo MakeCandidate(
        int jobCandidateId,
        int? openingId = null,
        string firstName = "John",
        string middleName = "Q",
        string lastName = "Doe")
    {
        return new CandidateInfo
        {
            JobCandidateID = jobCandidateId,
            OpeningID = openingId ?? 0,
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            Email = $"john{jobCandidateId}@example.com",
            PhoneNumber = "555-0000",
            NationalID = $"NID-{jobCandidateId:00000}",
            JobTitle = "Developer",
            Gender = "M",
            MaritalStatus = "S",
            BirthDate = DateTime.UtcNow.AddYears(-25),
            Comment = string.Empty // Non-null
        };
    }

    // ------------------------------------------------------------------
    // GET ALL
    // ------------------------------------------------------------------
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        await using var db = BuildContext();

        db.CandidateInfos.Add(MakeCandidate(1));
        db.CandidateInfos.Add(MakeCandidate(2));
        await db.SaveChangesAsync();

        var mapper = FakeMapper();
        var controller = new CandidateInfoController(db, mapper);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<CandidateInfo>>(ok.Value);

        Assert.Equal(2, list.Count);
    }

    // ------------------------------------------------------------------
    // GET BY ID
    // ------------------------------------------------------------------
    [Fact]
    public async Task GetById_ReturnsOk()
    {
        await using var db = BuildContext();

        var entity = MakeCandidate(10, firstName: "John");
        db.CandidateInfos.Add(entity);
        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        A.CallTo(() => mapper.Map<CandidateInfoDTO>(entity))
            .Returns(new CandidateInfoDTO
            {
                JobCandidateID = 10,
                FirstName = "John"
            });

        var controller = new CandidateInfoController(db, mapper);

        var result = await controller.Get(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CandidateInfoDTO>(ok.Value);

        Assert.Equal(10, dto.JobCandidateID);
        Assert.Equal("John", dto.FirstName);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new CandidateInfoController(db, mapper);

        var result = await controller.Get(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ------------------------------------------------------------------
    // GET BY OPENING
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetByOpening_ReturnsList()
    {
        await using var db = BuildContext();

        db.CandidateInfos.AddRange(
            MakeCandidate(1, openingId: 50, firstName: "A"),
            MakeCandidate(2, openingId: 50, firstName: "B"),
            MakeCandidate(3, openingId: 60, firstName: "C")
        );
        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        // Keep your fake mapping; proxies may be returned
        A.CallTo(() => mapper.Map<List<CandidateInfoDTO>>(A<List<CandidateInfo>>._))
            .Returns(new List<CandidateInfoDTO>
            {
                new CandidateInfoDTO { JobCandidateID = 1 },
                new CandidateInfoDTO { JobCandidateID = 2 }
            });

        var controller = new CandidateInfoController(db, mapper);

        var result = await controller.GetByOpening(50);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dtos = Assert.IsAssignableFrom<List<CandidateInfoDTO>>(ok.Value); // <—

        Assert.Equal(2, dtos.Count);
        Assert.Contains(dtos, d => d.JobCandidateID == 1);
        Assert.Contains(dtos, d => d.JobCandidateID == 2);
    }


    // ------------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------------
    [Fact]
    public async Task Create_ReturnsCreated()
    {
        await using var db = BuildContext();

        // Simulate existing JobCandidate
        db.JobCandidates.Add(new JobCandidate { JobCandidateID = 22 });
        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        var input = new CandidateInfoDTO
        {
            JobCandidateID = 22,
            FirstName = "John",
            BirthDate = DateTime.UtcNow.AddYears(-20)
        };

        // Mapper must return a fully valid entity (EF will save it)
        var mappedEntity = MakeCandidate(
            jobCandidateId: 22,
            firstName: "John"
        );
        mappedEntity.BirthDate = input.BirthDate!.Value;

        A.CallTo(() => mapper.Map<CandidateInfo>(input))
            .Returns(mappedEntity);

        A.CallTo(() => mapper.Map<CandidateInfoDTO>(mappedEntity))
            .Returns(new CandidateInfoDTO
            {
                JobCandidateID = 22,
                FirstName = "John",
                BirthDate = mappedEntity.BirthDate
            });

        var controller = new CandidateInfoController(db, mapper);

        var result = await controller.Create(input);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<CandidateInfoDTO>(created.Value);

        Assert.Equal(22, dto.JobCandidateID);
        Assert.Equal("John", dto.FirstName);

        Assert.Single(db.CandidateInfos);
        Assert.Equal(22, db.CandidateInfos.Single().JobCandidateID);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenBirthdateInvalid()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();
        var controller = new CandidateInfoController(db, mapper);

        var input = new CandidateInfoDTO
        {
            JobCandidateID = 1,
            BirthDate = DateTime.MaxValue // invalid
        };

        var result = await controller.Create(input);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("BirthDate is out of range.", bad.Value);
    }

    // ------------------------------------------------------------------
    // PATCH
    // ------------------------------------------------------------------
    [Fact]
    public async Task Patch_ReturnsOk()
    {
        await using var db = BuildContext();

        var entity = MakeCandidate(10, firstName: "OldName");
        db.CandidateInfos.Add(entity);
        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        A.CallTo(() => mapper.Map<CandidateInfoDTO>(entity))
            .Returns(new CandidateInfoDTO
            {
                JobCandidateID = 10,
                FirstName = "NewName"
            });

        var controller = new CandidateInfoController(db, mapper);

        var dto = new CandidateInfoDTO
        {
            JobCandidateID = 10,
            FirstName = "NewName"
        };

        var result = await controller.Patch(10, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<CandidateInfoDTO>(ok.Value);

        Assert.Equal("NewName", returned.FirstName);

        // Confirm DB updated
        var fromDb = await db.CandidateInfos.FirstAsync(x => x.JobCandidateID == 10);
        Assert.Equal("NewName", fromDb.FirstName);
    }

    [Fact]
    public async Task Patch_ReturnsNotFound_WhenMissing()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new CandidateInfoController(db, mapper);

        var dto = new CandidateInfoDTO { JobCandidateID = 999 };

        var result = await controller.Patch(999, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Patch_ReturnsBadRequest_WhenIdMismatch()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new CandidateInfoController(db, mapper);

        var dto = new CandidateInfoDTO { JobCandidateID = 5 };

        var result = await controller.Patch(10, dto);

        Assert.IsType<BadRequestResult>(result);
    }

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        await using var db = BuildContext();

        // Insert a fully-valid candidate (per your model’s required fields)
        db.CandidateInfos.Add(MakeCandidate(10)); // JobCandidateID = 10
        await db.SaveChangesAsync();

        // IMPORTANT: EF-generated primary key is 'ID', not JobCandidateID
        var saved = await db.CandidateInfos.SingleAsync();

        var mapper = FakeMapper();
        var controller = new CandidateInfoController(db, mapper);

        // Pass the EF primary key to Delete
        var result = await controller.Delete(saved.ID);

        Assert.IsType<NoContentResult>(result);

        Assert.Empty(db.CandidateInfos);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new CandidateInfoController(db, mapper);

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
