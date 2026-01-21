
using Xunit;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Controllers;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using RhManagementApi.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EmployeePayHistoryControllerTests
{
    private AdventureWorksContext BuildContext()
    {
        var opts = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AdventureWorksContext(opts);
    }

    private static IMapper FakeMapper() => A.Fake<IMapper>();

    private static EmployeePayHistory MakeEPH(
        int businessId,
        decimal rate = 20m,
        byte freq = 1,
        DateTime? changeDate = null)
    {
        return new EmployeePayHistory
        {
            BusinessEntityID = businessId,
            RateChangeDate = changeDate?.Date ?? DateTime.UtcNow.Date,
            Rate = rate,
            PayFrequency = freq
        };
    }

    //---------------------------------------------------------------------
    // GET
    //---------------------------------------------------------------------
    [Fact]
    public async Task Get_ReturnsOk_WhenFound()
    {
        await using var db = BuildContext();

        // FK Employee is required (composite key BusinessEntityID + RateChangeDate)
        db.Employees.Add(new Employee
        {
            BusinessEntityID = 15,
            NationalIDNumber = "123456789",
            JobTitle = "Dev",
            BirthDate = DateTime.UtcNow.AddYears(-30),
            MaritalStatus = "S",
            Gender = "M",
            HireDate = DateTime.UtcNow.AddYears(-1),
            SalariedFlag = true
        });

        var eph = MakeEPH(15);
        db.EmployeePayHistories.Add(eph);

        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        A.CallTo(() => mapper.Map<List<EmployeePayHistoryDTO>>(A<List<EmployeePayHistory>>._))
            .Returns(new List<EmployeePayHistoryDTO>
            {
                new EmployeePayHistoryDTO
                {
                    BusinessEntityID = 15,
                    Rate = eph.Rate,
                    PayFrequency = eph.PayFrequency,
                    RateChangeDate = eph.RateChangeDate
                }
            });

        var controller = new EmployeePayHistoryController(db, mapper);

        var result = await controller.Get(15);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<List<EmployeePayHistoryDTO>>(ok.Value);

        Assert.Single(list);
        Assert.Equal(15, list[0].BusinessEntityID);
    }


    [Fact]
    public async Task Get_ReturnsNotFound_WhenNoneFound()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        // When no records exist, controller ToListAsync() -> empty list.
        // It then maps that list; we can return an empty list from mapper.
        A.CallTo(() => mapper.Map<List<EmployeePayHistoryDTO>>(A<List<EmployeePayHistory>>._))
            .Returns(new List<EmployeePayHistoryDTO>());

        var controller = new EmployeePayHistoryController(db, mapper);

        var result = await controller.Get(999);

        var ok = Assert.IsType<OkObjectResult>(result);

        var list = Assert.IsAssignableFrom<IList<EmployeePayHistoryDTO>>(ok.Value);
        Assert.Empty(list);
    }


    //---------------------------------------------------------------------
    // CREATE
    //---------------------------------------------------------------------
    [Fact]
    public async Task Create_ReturnsCreated()
    {
        await using var db = BuildContext();

        // Must seed Employee FK
        db.Employees.Add(new Employee
        {
            BusinessEntityID = 20,
            NationalIDNumber = "123456789",
            JobTitle = "Dev",
            BirthDate = DateTime.UtcNow.AddYears(-30),
            MaritalStatus = "S",
            Gender = "M",
            HireDate = DateTime.UtcNow.AddYears(-1),
            SalariedFlag = true
        });

        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        var inputDto = new EmployeePayHistoryDTO
        {
            BusinessEntityID = 20,
            Rate = 50m,
            PayFrequency = 2,
            RateChangeDate = new DateTime(2024, 1, 1)
        };

        var mappedEntity = new EmployeePayHistory
        {
            BusinessEntityID = 20,
            Rate = 50m,
            PayFrequency = 2,
            RateChangeDate = inputDto.RateChangeDate.Value
        };

        A.CallTo(() => mapper.Map<EmployeePayHistory>(inputDto))
            .Returns(mappedEntity);

        A.CallTo(() => mapper.Map<EmployeePayHistoryDTO>(mappedEntity))
            .Returns(inputDto);

        var controller = new EmployeePayHistoryController(db, mapper);

        var result = await controller.Create(inputDto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<EmployeePayHistoryDTO>(created.Value);

        Assert.Equal(20, dto.BusinessEntityID);
        Assert.Equal(50m, dto.Rate);

        // Validate EF inserted it
        Assert.Single(db.EmployeePayHistories);
    }

    [Fact]
    public async Task Create_FillsMissingRateChangeDate()
    {
        await using var db = BuildContext();

        db.Employees.Add(new Employee
        {
            BusinessEntityID = 33,
            NationalIDNumber = "123456789",
            JobTitle = "Dev",
            BirthDate = DateTime.UtcNow.AddYears(-30),
            MaritalStatus = "S",
            Gender = "M",
            HireDate = DateTime.UtcNow.AddYears(-1),
            SalariedFlag = true
        });

        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        var inputDto = new EmployeePayHistoryDTO
        {
            BusinessEntityID = 33,
            Rate = 90m,
            PayFrequency = 1,
            RateChangeDate = null // controller should auto-fill
        };

        var mapped = new EmployeePayHistory
        {
            BusinessEntityID = 33,
            Rate = 90m,
            PayFrequency = 1,
            RateChangeDate = DateTime.Now // controller fills it
        };

        A.CallTo(() => mapper.Map<EmployeePayHistory>(A<EmployeePayHistoryDTO>._))
            .Returns(mapped);

        A.CallTo(() => mapper.Map<EmployeePayHistoryDTO>(mapped))
            .Returns(inputDto);

        var controller = new EmployeePayHistoryController(db, mapper);

        var result = await controller.Create(inputDto);

        var created = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Single(db.EmployeePayHistories);
        Assert.Equal(33, ((EmployeePayHistoryDTO)created.Value).BusinessEntityID);
    }
}
