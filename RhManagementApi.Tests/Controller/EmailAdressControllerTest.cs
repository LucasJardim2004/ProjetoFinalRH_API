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

public class EmailAddressControllerTest
{    
    [Fact]
    public async Task EmailAddressController_GetByID_GetOk()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

        await using var context = new AdventureWorksContext(options);

        context.EmailAddresses.AddRange(
            new PersonEmailAddress { BusinessEntityID = 1, EmailAddressID = 1, EmailAddress = "email@email.com" }
        );

        await context.SaveChangesAsync();

        var mapper = A.Fake<IMapper>();
        
        A.CallTo(() => mapper.Map<PersonEmailAddressDTO>(A<PersonEmailAddress>.That.Matches(o => o.BusinessEntityID == 1)))
            .Returns(new PersonEmailAddressDTO
            {
                BusinessEntityID = 1, 
                EmailAddressID = 1, 
                EmailAddress = "email@email.com"
            });

        var controller = new PersonEmailAddressController(context, mapper);

        //Act
        var result = await controller.Get(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var emailDto = Assert.IsType<PersonEmailAddressDTO>(ok.Value);
        Assert.Equal(1, emailDto.BusinessEntityID);
        Assert.Equal("email@email.com", emailDto.EmailAddress);
    }
    
    [Fact]
    public async Task OpeningController_GetByID_GetNotFound()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

        await using var context = new AdventureWorksContext(options);

        context.EmailAddresses.AddRange(
            new PersonEmailAddress { BusinessEntityID = 1, EmailAddressID = 1, EmailAddress = "email@email.com" }
        );

        await context.SaveChangesAsync();

        var mapper = A.Fake<IMapper>();
        
        A.CallTo(() => mapper.Map<PersonEmailAddressDTO>(A<PersonEmailAddress>.That.Matches(o => o.BusinessEntityID == 1)))
            .Returns(new PersonEmailAddressDTO
            {
                BusinessEntityID = 1, 
                EmailAddressID = 1, 
                EmailAddress = "email@email.com"
            });

        var controller = new PersonEmailAddressController(context, mapper);

        //Act
        var result = await controller.Get(2);

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
        var inputDto = new PersonEmailAddressDTO
        {
            BusinessEntityID = 1,
            EmailAddress = "email@email.com"
        };

        // Object mapper should create
        var mappedEmailAddress = new PersonEmailAddress
        {
            BusinessEntityID = 1, 
            EmailAddressID = 1, 
            EmailAddress = "email@email.com"
        };

        // Mapper DTO → entity
        A.CallTo(() => mapper.Map<PersonEmailAddress>(A<PersonEmailAddressDTO>.Ignored))
            .Returns(mappedEmailAddress);

        // Mapper entity → DTO (returned in CreatedAtAction)
        A.CallTo(() => mapper.Map<PersonEmailAddressDTO>(A<PersonEmailAddress>.Ignored))
            .Returns(new PersonEmailAddressDTO
            {
                BusinessEntityID = 1, 
                EmailAddressID = 1, 
                EmailAddress = "email@email.com"
            });

        var controller = new PersonEmailAddressController(context, mapper);

        // Act
        var result = await controller.Create(inputDto);

        // Assert — HTTP response
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PersonEmailAddressController.Get), created.ActionName);

        // Assert — Payload type
        var returnedDto = Assert.IsType<PersonEmailAddressDTO>(created.Value);

        // Assert — Payload content
        Assert.Equal(1, returnedDto.BusinessEntityID);
        Assert.Equal("email@email.com", returnedDto.EmailAddress);
        Assert.Equal(1, returnedDto.EmailAddressID);

        // Assert — Persisted in database
        Assert.Equal(1, context.EmailAddresses.Count());

        var savedEmails = context.EmailAddresses.First();
        Assert.Equal("email@email.com", savedEmails.EmailAddress);
    }


    [Fact]
    public async Task PersonEmailAddressController_CreateWithoutEmail_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);
        var mapper = A.Fake<IMapper>();

        var inputDto = new PersonEmailAddressDTO
        {
            BusinessEntityID = 1,
            EmailAddress = null   // Missing required field
        };

        var controller = new PersonEmailAddressController(context, mapper);

        // Act
        var result = await controller.Create(inputDto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email Address is required", badRequest.Value);

        // Ensure DB not modified
        Assert.Empty(context.EmailAddresses);
    }


    [Fact]
    public async Task PersonEmailAddressController_Patch_ReturnsOk()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);

        context.EmailAddresses.Add(new PersonEmailAddress
        {
            BusinessEntityID = 1,
            EmailAddressID = 1,
            EmailAddress = "old@email.com"
        });
        await context.SaveChangesAsync();

        var mapper = A.Fake<IMapper>();

        A.CallTo(() => mapper.Map<PersonEmailAddressDTO>(A<PersonEmailAddress>.Ignored))
            .Returns(new PersonEmailAddressDTO
            {
                BusinessEntityID = 1,
                EmailAddressID = 1,
                EmailAddress = "new@email.com"
            });

        var controller = new PersonEmailAddressController(context, mapper);

        var patchDto = new PersonEmailAddressDTO
        {
            BusinessEntityID = 1,
            EmailAddress = "new@email.com"
        };

        // Act
        var result = await controller.Patch(1, patchDto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var updatedDto = Assert.IsType<PersonEmailAddressDTO>(ok.Value);

        Assert.Equal("new@email.com", updatedDto.EmailAddress);

        var fromDb = await context.EmailAddresses.FindAsync(1,1);
        Assert.Equal("new@email.com", fromDb.EmailAddress);
    }
    
    [Fact]
    public async Task PersonEmailAddressController_Patch_IdMismatch_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);

        var mapper = A.Fake<IMapper>();

        var controller = new PersonEmailAddressController(context, mapper);

        var patchDto = new PersonEmailAddressDTO
        {
            BusinessEntityID = 2, // DTO ID ≠ Route ID
            EmailAddress = "new@email.com"
        };

        // Act
        var result = await controller.Patch(1, patchDto);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }
}
