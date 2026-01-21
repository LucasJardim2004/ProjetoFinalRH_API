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

public class PhoneControllerTest
{
    
    [Fact]
    public async Task PhoneController_GetByID_GetOk()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

        await using var context = new AdventureWorksContext(options);

        context.PeoplePhones.AddRange(
            new PersonPhone { BusinessEntityID = 1, PhoneNumber = "933944955", PhoneNumberTypeID = 1 }
        );

        await context.SaveChangesAsync();

        var mapper = A.Fake<IMapper>();

        
        A.CallTo(() => mapper.Map<PersonPhoneDTO>(A<PersonPhone>.That.Matches(o => o.BusinessEntityID == 1)))
            .Returns(new PersonPhoneDTO
            {
                BusinessEntityID = 1,
                PhoneNumber = "933944955",
                PhoneNumberTypeID = 1
            });

        var controller = new PersonPhoneController(context, mapper);

        //Act
        var result = await controller.Get(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var personPhoneDTO = Assert.IsType<PersonPhoneDTO>(ok.Value);
        Assert.Equal(1, personPhoneDTO.BusinessEntityID);
        Assert.Equal("933944955", personPhoneDTO.PhoneNumber);
    }

    [Fact]
    public async Task PhoneController_GetByID_GetNotFound()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

        await using var context = new AdventureWorksContext(options);

        context.PeoplePhones.AddRange(
            new PersonPhone { BusinessEntityID = 1, PhoneNumber = "933944955", PhoneNumberTypeID = 1 }
        );

        await context.SaveChangesAsync();

        var mapper = A.Fake<IMapper>();

        
        A.CallTo(() => mapper.Map<PersonPhoneDTO>(A<PersonPhone>.That.Matches(o => o.BusinessEntityID == 1)))
            .Returns(new PersonPhoneDTO
            {
                BusinessEntityID = 1,
                PhoneNumber = "933944955",
                PhoneNumberTypeID = 1
            });

        var controller = new PersonPhoneController(context, mapper);

        //Act
        var result = await controller.Get(2);

        //Act
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PhoneController_Create_ReturnsCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);

        var mapper = A.Fake<IMapper>();

        // Input JSON equivalent
        var inputDto = new PersonPhoneDTO
        {
            BusinessEntityID = 1,
            PhoneNumber = "933944955",
            PhoneNumberTypeID = 1
        };

        // Object mapper should create
        var mappedOpening = new PersonPhone
        {
            BusinessEntityID = 1,
            PhoneNumber = "933944955",
            PhoneNumberTypeID = 1
        };

        // Mapper DTO → entity
        A.CallTo(() => mapper.Map<PersonPhone>(A<PersonPhoneDTO>.Ignored))
            .Returns(mappedOpening);

        // Mapper entity → DTO (returned in CreatedAtAction)
        A.CallTo(() => mapper.Map<PersonPhoneDTO>(A<PersonPhone>.Ignored))
            .Returns(new PersonPhoneDTO
            {
                BusinessEntityID = 1,
                PhoneNumber = "933944955",
                PhoneNumberTypeID = 1
            });

        var controller = new PersonPhoneController(context, mapper);

        // Act
        var result = await controller.Create(inputDto);

        // Assert — HTTP response
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PersonPhoneController.Get), created.ActionName);

        // Assert — Payload type
        var returnedDto = Assert.IsType<PersonPhoneDTO>(created.Value);

        // Assert — Payload content
        Assert.Equal(1, returnedDto.BusinessEntityID);
        Assert.Equal("933944955", returnedDto.PhoneNumber);
        Assert.Equal(1, returnedDto.PhoneNumberTypeID);

        // Assert — Persisted in database
        Assert.Equal(1, context.PeoplePhones.Count());

        var savedPhones = context.PeoplePhones.First();
        Assert.Equal(1, savedPhones.BusinessEntityID);
    }

    
    [Fact]
    public async Task PhoneController_CreateWithoutID_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AdventureWorksContext(options);

        var mapper = A.Fake<IMapper>();

        var inputDto = new PersonPhoneDTO
        {
            PhoneNumber = "933944955",
            PhoneNumberTypeID = 1
        };

        var controller = new PersonPhoneController(context, mapper);

        // Act
        var result = await controller.Create(inputDto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("BusinessEntityID is required", badRequest.Value);
    }
}
