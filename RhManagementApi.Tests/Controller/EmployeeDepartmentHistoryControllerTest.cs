
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
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable

public class EmployeeDepartmentHistoryControllerTests
{
    private AdventureWorksContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AdventureWorksContext(options);
    }

    private static IMapper FakeMapper() => A.Fake<IMapper>();

    private static Department MakeDepartment(short id) =>
        new Department
        {
            DepartmentID = id,
            Name = $"Dept{id}",
            GroupName = "Group",
            ModifiedDate = DateTime.UtcNow
        };

    private static EmployeeDepartmentHistory MakeEDH(
        int businessId,
        short deptId,
        DateTime start,
        DateTime? end = null)
    {
        return new EmployeeDepartmentHistory
        {
            BusinessEntityID = businessId,
            DepartmentID = deptId,
            StartDate = start,
            EndDate = end
        };
    }

    //--------------------------------------------------------------------
    // GET
    //--------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsOk_WhenHistoryExists()
    {
        await using var db = BuildContext();

        var dept = MakeDepartment(5);
        db.Departments.Add(dept);

        var start = new DateTime(2024, 1, 1);
        var edh = MakeEDH(15, 5, start);

        db.EmployeeDepartmentHistories.Add(edh);
        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        // Fake mapper for GET -> DTO
        A.CallTo(() => mapper.Map<EmployeeDepartmentHistoryDTO>(A<EmployeeDepartmentHistory>._))
            .ReturnsLazily(call =>
            {
                var e = call.GetArgument<EmployeeDepartmentHistory>(0);
                return new EmployeeDepartmentHistoryDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    DepartmentID = e.DepartmentID,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate
                };
            });

        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var result = await controller.Get(15);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<List<EmployeeDepartmentHistoryDTO>>(ok.Value);

        Assert.Single(list);
        Assert.Equal(15, list[0].BusinessEntityID);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenNone()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var result = await controller.Get(999);

        Assert.IsType<NotFoundResult>(result);
    }

    //--------------------------------------------------------------------
    // CREATE
    //--------------------------------------------------------------------

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        await using var db = BuildContext();

        // Seed department
        db.Departments.Add(MakeDepartment(5));
        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        var dto = new EmployeeDepartmentHistoryDTO
        {
            BusinessEntityID = 20,
            DepartmentID = 5,
            StartDate = new DateTime(2024, 1, 1)
        };

        // Fake Map<TEntity>
        A.CallTo(() => mapper.Map<EmployeeDepartmentHistory>(A<EmployeeDepartmentHistory>._))
            .ReturnsLazily(call =>
            {
                var d = call.GetArgument<EmployeeDepartmentHistory>(0);
                return new EmployeeDepartmentHistory
                {
                    BusinessEntityID = d.BusinessEntityID,
                    DepartmentID = d.DepartmentID,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate
                };
            });

        // Fake Map<DTO>
        A.CallTo(() => mapper.Map<EmployeeDepartmentHistoryDTO>(A<EmployeeDepartmentHistory>._))
            .ReturnsLazily(call =>
            {
                var e = call.GetArgument<EmployeeDepartmentHistory>(0);
                return new EmployeeDepartmentHistoryDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    DepartmentID = e.DepartmentID,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate
                };
            });

        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var returned = Assert.IsType<EmployeeDepartmentHistoryDTO>(created.Value);

        Assert.Equal(20, returned.BusinessEntityID);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenMissingBusinessEntityID()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();
        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var dto = new EmployeeDepartmentHistoryDTO
        {
            BusinessEntityID = null,
            DepartmentID = 5
        };

        var result = await controller.Create(dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("BusinessEntityID is required.", bad.Value);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDeptIdInvalid()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var dto = new EmployeeDepartmentHistoryDTO
        {
            BusinessEntityID = 10,
            DepartmentID = 99
        };

        var result = await controller.Create(dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("DepartmentID must be between 1 and 16.", bad.Value);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenEndDateInvalid()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();
        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var dto = new EmployeeDepartmentHistoryDTO
        {
            BusinessEntityID = 10,
            DepartmentID = 5,
            EndDate = DateTime.MaxValue
        };

        var result = await controller.Create(dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("EndDate is out of range.", bad.Value);
    }

    //--------------------------------------------------------------------
    // PATCH
    //--------------------------------------------------------------------

    [Fact]
    public async Task Patch_ReturnsOk()
    {
        await using var db = BuildContext();

        db.Departments.Add(MakeDepartment(3));

        var start = new DateTime(2024, 1, 1);
        var edh = MakeEDH(50, 3, start);

        db.EmployeeDepartmentHistories.Add(edh);
        await db.SaveChangesAsync();

        var mapper = FakeMapper();

        A.CallTo(() => mapper.Map<EmployeeDepartmentHistoryDTO>(A<EmployeeDepartmentHistory>._))
            .ReturnsLazily(call =>
            {
                var e = call.GetArgument<EmployeeDepartmentHistory>(0);
                return new EmployeeDepartmentHistoryDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    DepartmentID = e.DepartmentID,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate
                };
            });

        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var dto = new EmployeeDepartmentHistoryDTO
        {
            BusinessEntityID = 50,
            EndDate = new DateTime(2025, 1, 1)
        };

        var result = await controller.Patch(50, start, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<EmployeeDepartmentHistoryDTO>(ok.Value);

        Assert.Equal(new DateTime(2025, 1, 1), returned.EndDate);
    }

    [Fact]
    public async Task Patch_ReturnsNotFound()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var dto = new EmployeeDepartmentHistoryDTO
        {
            BusinessEntityID = 10,
            EndDate = DateTime.UtcNow
        };

        var result = await controller.Patch(10, DateTime.UtcNow.Date, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Patch_ReturnsBadRequest_WhenIdMismatch()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var dto = new EmployeeDepartmentHistoryDTO
        {
            BusinessEntityID = 99
        };

        var result = await controller.Patch(100, DateTime.UtcNow.Date, dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ID mismatch", bad.Value);
    }

    [Fact]
    public async Task Patch_ReturnsBadRequest_WhenEndDateInvalid()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new EmployeeDepartmentHistoryController(db, mapper);

        var dto = new EmployeeDepartmentHistoryDTO
        {
            BusinessEntityID = 10,
            EndDate = DateTime.MaxValue
        };

        var result = await controller.Patch(10, DateTime.UtcNow.Date, dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("EndDate is out of range.", bad.Value);
    }
}
