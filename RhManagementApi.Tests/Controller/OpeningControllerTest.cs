using Xunit;
using AutoMapper;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Controllers;
using RhManagementApi.Data;
using RhManagementApi.Models;
using RhManagementApi.DTOs;
using System.Reflection;

public class OpeningControllerTest
{
    [Fact]
    public async Task OpeningController_GetAll_ReturnsOk()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);

        context.Openings.AddRange(
            new Opening { OpeningID = 1, JobTitle = "Developer" },
            new Opening { OpeningID = 2, JobTitle = "Analyst" }
        );

        await context.SaveChangesAsync();

        var mapper = A.Fake<IMapper>();

        var controller = new OpeningController(context, mapper);

        // Act
        var result = await controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var openings = Assert.IsAssignableFrom<IEnumerable<Opening>>(ok.Value);

        Assert.Equal(2, openings.Count());
    }

    [Fact]
    public async Task OpeningController_GetByID_GetOk()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

        await using var context = new AdventureWorksContext(options);

        context.Openings.AddRange(
            new Opening { OpeningID = 1, JobTitle = "Developer" }
        );

        await context.SaveChangesAsync();

        var mapper = A.Fake<IMapper>();

        
        A.CallTo(() => mapper.Map<OpeningDTO>(A<Opening>.That.Matches(o => o.OpeningID == 1)))
            .Returns(new OpeningDTO
            {
                OpeningID = 1,
                JobTitle = "Developer"
            });

        var controller = new OpeningController(context, mapper);

        //Act
        var result = await controller.Get(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var openingDto = Assert.IsType<OpeningDTO>(ok.Value);
        Assert.Equal(1, openingDto.OpeningID);
        Assert.Equal("Developer", openingDto.JobTitle);
    }

    [Fact]
    public async Task OpeningController_GetByID_GetNotFound()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

        await using var context = new AdventureWorksContext(options);

        context.Openings.AddRange(
            new Opening { OpeningID = 1, JobTitle = "Developer" }
        );

        await context.SaveChangesAsync();

        var mapper = A.Fake<IMapper>();

        
        A.CallTo(() => mapper.Map<OpeningDTO>(A<Opening>.That.Matches(o => o.OpeningID == 1)))
            .Returns(new OpeningDTO
            {
                OpeningID = 1,
                JobTitle = "Developer"
            });

        var controller = new OpeningController(context, mapper);

        //Arrange
        var result = await controller.Get(999);

        //Act
        Assert.IsType<NotFoundResult>(result);
    }


    [Fact]
    public async Task OpeningController_Create_ReturnsCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);

        var mapper = A.Fake<IMapper>();

        // Input JSON equivalent
        var inputDto = new OpeningDTO
        {
            JobTitle = "SCRUM Master",
            Description = "Chefe disto tudo"
        };

        // Object mapper should create
        var mappedOpening = new Opening
        {
            OpeningID = 1,            
            JobTitle = inputDto.JobTitle,
            Description = inputDto.Description,
            DateCreated = DateTime.Now,  
            OpenFlag = true
        };

        // Mapper DTO → entity
        A.CallTo(() => mapper.Map<Opening>(A<OpeningDTO>.Ignored))
            .Returns(mappedOpening);

        // Mapper entity → DTO (returned in CreatedAtAction)
        A.CallTo(() => mapper.Map<OpeningDTO>(A<Opening>.Ignored))
            .Returns(new OpeningDTO
            {
                OpeningID = 1,
                JobTitle = "SCRUM Master",
                Description = "Chefe disto tudo",
                OpenFlag = true
            });

        var controller = new OpeningController(context, mapper);

        // Act
        var result = await controller.Create(inputDto);

        // Assert — HTTP response
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(OpeningController.Get), created.ActionName);

        // Assert — Payload type
        var returnedDto = Assert.IsType<OpeningDTO>(created.Value);

        // Assert — Payload content
        Assert.Equal(1, returnedDto.OpeningID);
        Assert.Equal("SCRUM Master", returnedDto.JobTitle);
        Assert.Equal("Chefe disto tudo", returnedDto.Description);
        Assert.True(returnedDto.OpenFlag);

        // Assert — Persisted in database
        Assert.Equal(1, context.Openings.Count());

        var savedOpening = context.Openings.First();
        Assert.Equal("SCRUM Master", savedOpening.JobTitle);
        Assert.True(savedOpening.OpenFlag);
    }

    [Fact]
    public async Task OpeningController_CreateWithoutTitle_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);

        var mapper = A.Fake<IMapper>();

        var inputDto = new OpeningDTO
        {
            Description = "New Epic Openning."
        };

        var controller = new OpeningController(context, mapper);

        // Act
        var result = await controller.Create(inputDto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Job Title is required", badRequest.Value);
    }



    [Fact]
    public async Task Delete_WhenOpeningExists_RemovesEntityAndReturnsNoContent()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);
        context.Openings.Add(new Opening { OpeningID = 1, JobTitle = "Developer" });
        await context.SaveChangesAsync();

        // Mapper not used by Delete → a fake is fine but unnecessary
        var mapper = A.Fake<IMapper>();
        var controller = new OpeningController(context, mapper);

        // Act
        var result = await controller.Delete(1);

        // Assert: result type
        Assert.IsType<NoContentResult>(result); // or OkResult/OkObjectResult depending on your controller

        // Assert: entity actually gone
        var fromDb = await context.Openings.FindAsync(1);
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task Delete_WhenOpeningDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);
        var mapper = A.Fake<IMapper>();
        var controller = new OpeningController(context, mapper);

        // Act
        var result = await controller.Delete(42);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
