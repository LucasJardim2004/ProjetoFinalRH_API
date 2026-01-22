
// EmployeeControllerTests.cs — Fully Patched + Reflection Fixes + Constructor Fixes

using Xunit;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RhManagementApi.Controllers;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using RhManagementApi.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RhManagementApi.Constants;
using Microsoft.EntityFrameworkCore.Diagnostics;

#pragma warning disable // Disable test warnings (nullability, proxies, etc.)

public class EmployeeControllerTests
{
    // -------------------------------------------------------
    // DB Context Builders
    // -------------------------------------------------------

    private AdventureWorksContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AdventureWorksContext(options);
    }

    private AuthDbContext BuildAuthContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthDbContext(options);
    }

    private static IMapper FakeMapper() => A.Fake<IMapper>();

    // -------------------------------------------------------
    // Helpers to Build Test Entities
    // -------------------------------------------------------

    private static Employee MakeEmployee(
        int beId,
        string jobTitle = "Developer",
        string nationalId = "NID-123456",
        string marital = "S",
        string gender = "M",
        DateTime? birth = null,
        DateTime? hire = null)
    {
        return new Employee
        {
            BusinessEntityID = beId,
            JobTitle = jobTitle,
            NationalIDNumber = nationalId,
            MaritalStatus = marital,
            Gender = gender,
            BirthDate = birth ?? DateTime.UtcNow.AddYears(-30),
            HireDate = hire ?? DateTime.UtcNow.AddYears(-1),
            SalariedFlag = true,
            VacationHours = 0,
            SickLeaveHours = 0,
            CurrentFlag = true
        };
    }

    private static Person MakePerson(int id, string first = "John", string last = "Doe")
        => new Person
        {
            BusinessEntityID = id,
            PersonType = "EM",
            FirstName = first,
            LastName = last
        };

    private static PersonEmailAddress MakeEmail(int id, string email = "john.doe@example.com")
        => new PersonEmailAddress { BusinessEntityID = id, EmailAddress = email };

    private static PersonPhone MakePhone(int id, string phone = "555-0000")
        => new PersonPhone { BusinessEntityID = id, PhoneNumber = phone, PhoneNumberTypeID = 1 };


    private static void SetUser(EmployeeController controller, string role, int? beId = null)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
        if (beId.HasValue)
            claims.Add(new Claim("business_entity_id", beId.Value.ToString()));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };
    }

    // -------------------------------------------------------
    // GETALL TEST 1
    // -------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsPagedResults_WithSearch()
    {
        await using var db = BuildContext();
        await using var auth = BuildAuthContext();
        var mapper = FakeMapper();

        db.Employees.AddRange(
            MakeEmployee(1, jobTitle: "Developer", nationalId: "NID-1"),
            MakeEmployee(2, jobTitle: "Senior Developer", nationalId: "NID-2"),
            MakeEmployee(3, jobTitle: "HR Specialist", nationalId: "NID-3")
        );
        await db.SaveChangesAsync();

        A.CallTo(() => mapper.Map<List<EmployeeDTO>>(A<object>._))
            .ReturnsLazily(call =>
            {
                var list = call.Arguments[0] as List<Employee> ?? new List<Employee>();
                return list.Select(e => new EmployeeDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    JobTitle = e.JobTitle
                }).ToList();
            });

        var controller = new EmployeeController(db, auth, mapper);
        SetUser(controller, RoleNames.HR, 999);

        var result = await controller.GetAll(1, 2, "developer");
        var ok = Assert.IsType<OkObjectResult>(result);

        var payload = ok.Value!;
        var type = payload.GetType();

        var data = type.GetProperty("data")!.GetValue(payload) as IList<EmployeeDTO>;
        var pagination = type.GetProperty("pagination")!.GetValue(payload)!;

        var total = (int)pagination.GetType().GetProperty("totalCount")!.GetValue(pagination)!;

        Assert.Equal(2, data.Count);
        Assert.Equal(2, total);
    }

    // -------------------------------------------------------
    // GETALL TEST 2
    // -------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsPaginationMetadata_WhenNoSearch()
    {
        await using var db = BuildContext();
        await using var auth = BuildAuthContext();
        var mapper = FakeMapper();

        for (int i = 1; i <= 5; i++)
            db.Employees.Add(MakeEmployee(i));

        await db.SaveChangesAsync();

        A.CallTo(() => mapper.Map<List<EmployeeDTO>>(A<object>._))
            .ReturnsLazily(call =>
            {
                var src = call.Arguments[0] as List<Employee> ?? new List<Employee>();
                return src.Select(e => new EmployeeDTO { BusinessEntityID = e.BusinessEntityID }).ToList();
            });

        var controller = new EmployeeController(db, auth, mapper);
        SetUser(controller, RoleNames.HR, 1);

        var result = await controller.GetAll(2, 2, null);
        var ok = Assert.IsType<OkObjectResult>(result);

        var payload = ok.Value!;
        var t = payload.GetType();

        var pagination = t.GetProperty("pagination")!.GetValue(payload)!;

        Assert.Equal(5, (int)pagination.GetType().GetProperty("totalCount")!.GetValue(pagination)!);
        Assert.Equal(3, (int)pagination.GetType().GetProperty("totalPages")!.GetValue(pagination)!);
    }

    // -------------------------------------------------------
    // GET BY ID
    // -------------------------------------------------------

    [Fact]
    public async Task Get_ById_ReturnsOk_ForHR()
    {
        await using var db = BuildContext();
        await using var auth = BuildAuthContext();
        var mapper = FakeMapper();

        db.Employees.Add(MakeEmployee(10));
        db.People.Add(MakePerson(10, "John", "Smith"));
        db.PeoplePhones.Add(MakePhone(10, "555-0000"));
        db.EmailAddresses.Add(MakeEmail(10, "john.smith@company.com"));
        await db.SaveChangesAsync();

        A.CallTo(() => mapper.Map<EmployeeDTO>(A<object>._))
            .Returns(new EmployeeDTO { BusinessEntityID = 10 });

        var controller = new EmployeeController(db, auth, mapper);
        SetUser(controller, RoleNames.HR, 999);

        var result = await controller.Get(10);
        var ok = Assert.IsType<OkObjectResult>(result);

        var payload = ok.Value!;
        var t = payload.GetType();

        var phone = (string)t.GetProperty("PhoneNumber")!.GetValue(payload)!;
        var email = (string)t.GetProperty("EmailAddress")!.GetValue(payload)!;

        Assert.Equal("555-0000", phone);
        Assert.Equal("john.smith@company.com", email);
    }

    // -------------------------------------------------------
    // PATCH TESTS
    // -------------------------------------------------------

    [Fact]
    public async Task Patch_HR_ModifyingOther_AllowsAllFields()
    {
        await using var db = BuildContext();
        await using var auth = BuildAuthContext();
        var mapper = FakeMapper();

        db.Employees.Add(MakeEmployee(10));
        await db.SaveChangesAsync();

        A.CallTo(() => mapper.Map<EmployeeDTO>(A<object>._))
            .ReturnsLazily(call =>
            {
            var e = call.Arguments[0] as Employee ?? new Employee();
            return new EmployeeDTO
            {
                BusinessEntityID = e.BusinessEntityID,
                JobTitle = e.JobTitle,
                Gender = e.Gender,
                MaritalStatus = e.MaritalStatus
            };
        });

        var controller = new EmployeeController(db, auth, mapper);
        SetUser(controller, RoleNames.HR, 99);

        var dto = new EmployeeDTO
        {
            BusinessEntityID = 10,
            JobTitle = "Lead Dev",
            Gender = "F",
            MaritalStatus = "M"
        };

        var result = await controller.Patch(10, dto);
        var ok = Assert.IsType<OkObjectResult>(result);

        var returned = Assert.IsAssignableFrom<EmployeeDTO>(ok.Value); // allow proxy
        Assert.Equal("Lead Dev", returned.JobTitle);
    }
}
