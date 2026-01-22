
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

public class EmployeeControllerTests
{

    private AdventureWorksContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AdventureWorksContext(options);
    }


    private static IMapper FakeMapper() => A.Fake<IMapper>();

    private static Employee MakeEmployee(
        int beId,
        string jobTitle = "Developer",
        string nationalId = "NID-123456",
        string marital = "S",
        string gender = "M",
        DateTime? birth = null,
        DateTime? hire = null,
        bool salaried = true)
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
            SalariedFlag = salaried,
            VacationHours = 0,
            SickLeaveHours = 0,
            CurrentFlag = true
        };
    }

    private static Person MakePerson(int beId, string firstName = "John", string lastName = "Doe", string personType = "EM")
    {
        return new Person
        {
            BusinessEntityID = beId,
            PersonType = personType,
            FirstName = firstName,
            LastName = lastName
        };
    }

    private static PersonEmailAddress MakeEmail(int beId, string email = "john.doe@example.com")
    {
        return new PersonEmailAddress
        {
            BusinessEntityID = beId,
            EmailAddress = email
        };
    }

    private static PersonPhone MakePhone(int beId, string phone = "555-0001", int phoneTypeId = 1)
    {
        return new PersonPhone
        {
            BusinessEntityID = beId,
            PhoneNumber = phone,
            PhoneNumberTypeID = phoneTypeId
        };
    }

    private static Department MakeDepartment(short id, string name = "Engineering")
    {
        return new Department
        {
            DepartmentID = id,
            Name = name,
            GroupName = "Group",
            ModifiedDate = DateTime.UtcNow
        };
    }

    private static void SetUser(EmployeeController controller, string role, int? beId = null)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
        if (beId.HasValue)
        {
            claims.Add(new Claim("business_entity_id", beId.Value.ToString()));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
        };
    }

    // --------------------------------------------------------------------
    // GET ALL (paged + search)
    // --------------------------------------------------------------------
    [Fact]
    public async Task GetAll_ReturnsPagedResults_WithSearch()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        // Seed employees
        db.Employees.AddRange(
            MakeEmployee(1, jobTitle: "Developer", nationalId: "NID-1"),
            MakeEmployee(2, jobTitle: "Senior Developer", nationalId: "NID-2"),
            MakeEmployee(3, jobTitle: "HR Specialist", nationalId: "NID-3")
        );
        await db.SaveChangesAsync();


        A.CallTo(() => mapper.Map<List<EmployeeDTO>>(A<object>.Ignored))
            .ReturnsLazily(call =>
            {
                var src = call.GetArgument<object>(0) as List<Employee> ?? new List<Employee>();
                return src.Select(e => new EmployeeDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    JobTitle = e.JobTitle,
                    NationalIDNumber = e.NationalIDNumber,
                    BirthDate = e.BirthDate,
                    Gender = e.Gender,
                    MaritalStatus = e.MaritalStatus,
                    HireDate = e.HireDate,
                }).ToList();
            });


        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.HR, 999); // HR authorized

        // pageNumber=1 pageSize=2 search="developer"
        var result = await controller.GetAll(1, 2, "developer");

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!;
        var dataProp = value.GetType().GetProperty("data");
        var paginationProp = value.GetType().GetProperty("pagination");
        Assert.NotNull(dataProp);
        Assert.NotNull(paginationProp);

        var data = Assert.IsAssignableFrom<IList<EmployeeDTO>>(dataProp!.GetValue(value));
        var pagination = paginationProp!.GetValue(value);
        var totalCount = (int)pagination!.GetType().GetProperty("totalCount")!.GetValue(pagination)!;
        var totalPages = (int)pagination!.GetType().GetProperty("totalPages")!.GetValue(pagination)!;

        // Only the two developer roles should be counted
        Assert.Equal(2, totalCount);
        Assert.Equal(2, data.Count); // page size = 2 covers all
        Assert.Equal(1, totalPages);
    }

    [Fact]
    public async Task GetAll_ReturnsPaginationMetadata_WhenNoSearch()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        // Seed 5 employees
        for (int i = 1; i <= 5; i++)
            db.Employees.Add(MakeEmployee(i, jobTitle: $"Role-{i}", nationalId: $"NID-{i}"));

        await db.SaveChangesAsync();


        A.CallTo(() => mapper.Map<List<EmployeeDTO>>(A<object>._))
            .ReturnsLazily(call =>
            {
                var src = call.GetArgument<object>(0) as List<Employee> ?? new List<Employee>();
                return src.Select(e => new EmployeeDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    JobTitle = e.JobTitle
                }).ToList();
            });


        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.HR, 100);

        var result = await controller.GetAll(pageNumber: 2, pageSize: 2, searchTerm: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;
        var pagination = payload.GetType().GetProperty("pagination")!.GetValue(payload)!;

        var pageNumber = (int)pagination.GetType().GetProperty("pageNumber")!.GetValue(pagination)!;
        var pageSize = (int)pagination.GetType().GetProperty("pageSize")!.GetValue(pagination)!;
        var totalCount = (int)pagination.GetType().GetProperty("totalCount")!.GetValue(pagination)!;
        var totalPages = (int)pagination.GetType().GetProperty("totalPages")!.GetValue(pagination)!;

        Assert.Equal(2, pageNumber);
        Assert.Equal(2, pageSize);
        Assert.Equal(5, totalCount);
        Assert.Equal(3, totalPages);

        var data = Assert.IsAssignableFrom<IList<EmployeeDTO>>(payload.GetType().GetProperty("data")!.GetValue(payload)!);
        Assert.Equal(2, data.Count); // page 2, size 2 -> 2 items
    }

    // --------------------------------------------------------------------
    // GET /{id}
    // --------------------------------------------------------------------
    [Fact]
    public async Task Get_ById_ReturnsOk_ForHR_AnyEmployee()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        db.Employees.Add(MakeEmployee(10));
        db.People.Add(MakePerson(10, "John", "Smith"));
        db.PeoplePhones.Add(MakePhone(10, "555-0000", 1));
        db.EmailAddresses.Add(MakeEmail(10, "john.smith@company.com"));
        await db.SaveChangesAsync();

        A.CallTo(() => mapper.Map<EmployeeDTO>(A<Employee>._))
            .Returns(new EmployeeDTO { BusinessEntityID = 10, JobTitle = "Developer" });

        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.HR, 999);

        var result = await controller.Get(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var anon = ok.Value!;
        // reflect anonymous payload
        var emp = anon.GetType().GetProperty("Employee")!.GetValue(anon)!;
        var phone = anon.GetType().GetProperty("PhoneNumber")!.GetValue(anon) as string;
        var email = anon.GetType().GetProperty("EmailAddress")!.GetValue(anon) as string;
        var first = anon.GetType().GetProperty("FirstName")!.GetValue(anon) as string;
        var last = anon.GetType().GetProperty("LastName")!.GetValue(anon) as string;

        Assert.Equal("555-0000", phone);
        Assert.Equal("john.smith@company.com", email);
        Assert.Equal("John", first);
        Assert.Equal("Smith", last);

        var dto = Assert.IsType<EmployeeDTO>(emp);
        Assert.Equal(10, dto.BusinessEntityID);
    }

    [Fact]
    public async Task Get_ById_ReturnsForbid_WhenEmployeeTriesToViewOther()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();
        db.Employees.Add(MakeEmployee(1));
        db.Employees.Add(MakeEmployee(2));
        await db.SaveChangesAsync();

        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.Employee, beId: 1);

        var result = await controller.Get(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Get_ById_ReturnsNotFound_WhenMissing()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.HR, 999);

        var result = await controller.Get(9999);

        Assert.IsType<NotFoundResult>(result);
    }

    // --------------------------------------------------------------------
    // POST (Create)
    // --------------------------------------------------------------------
    [Fact]
    public async Task Create_Succeeds_AndCreatesGraph()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        // Department referenced in EDH (InMemory won't enforce FKs but it's correct to seed)
        db.Departments.Add(MakeDepartment(1));
        await db.SaveChangesAsync();

        var dto = new EmployeeWithPersonDTO
        {
            PersonType = "EM",
            FirstName = "Jane",
            LastName = "Doe",
            EmailAddress = "jane.doe@company.com",
            PhoneNumber = "555-7777",
            DepartmentId = 1,
            EmployeeDTO = new EmployeeDTO
            {
                JobTitle = "Analyst",
                NationalIDNumber = "NID-0001",
                BirthDate = DateTime.UtcNow.AddYears(-25),
                Gender = "F",
                MaritalStatus = "S",
                HireDate = DateTime.UtcNow.AddDays(-1),
            }
        };

        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.HR, 100);

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var returned = Assert.IsType<EmployeeDTO>(created.Value);

        // Check that all entities were inserted
        Assert.True(returned.BusinessEntityID.HasValue);
        var beId = returned.BusinessEntityID!.Value;

        Assert.NotNull(await db.BusinessEntities.FindAsync(beId));
        Assert.NotNull(await db.People.FindAsync(beId));
        Assert.True(await db.EmailAddresses.AnyAsync(x => x.BusinessEntityID == beId));
        Assert.True(await db.PeoplePhones.AnyAsync(x => x.BusinessEntityID == beId));
        Assert.NotNull(await db.Employees.FindAsync(beId));
        Assert.True(await db.EmployeeDepartmentHistories.AnyAsync(x => x.BusinessEntityID == beId));
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_OnValidation()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();
        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.HR, 100);

        // 1) Null body
        var res1 = await controller.Create(null);
        var bad1 = Assert.IsType<BadRequestObjectResult>(res1);
        Assert.Equal("Request body is required.", bad1.Value);

        // 2) Invalid PersonType
        var res2 = await controller.Create(new EmployeeWithPersonDTO
        {
            PersonType = "EMP", // invalid (length != 2)
            FirstName = "X",
            LastName = "Y",
            EmailAddress = "x@y",
            PhoneNumber = "1",
            DepartmentId = 1,
            EmployeeDTO = new EmployeeDTO { JobTitle = "Dev", NationalIDNumber = "N", Gender = "M", MaritalStatus = "S" }
        });
        var bad2 = Assert.IsType<BadRequestObjectResult>(res2);
        Assert.Equal("PersonType must be 2 characters (e.g., 'EM').", bad2.Value);

        // 3) BirthDate in future
        var res3 = await controller.Create(new EmployeeWithPersonDTO
        {
            PersonType = "EM",
            FirstName = "X",
            LastName = "Y",
            EmailAddress = "x@y",
            PhoneNumber = "1",
            DepartmentId = 1,
            EmployeeDTO = new EmployeeDTO
            {
                JobTitle = "Dev",
                NationalIDNumber = "N",
                Gender = "M",
                MaritalStatus = "S",
                BirthDate = DateTime.UtcNow.AddDays(1) // future
            }
        });
        var bad3 = Assert.IsType<BadRequestObjectResult>(res3);
        Assert.Equal("BirthDate cannot be in the future.", bad3.Value);
    }

    // --------------------------------------------------------------------
    // PATCH
    // --------------------------------------------------------------------
    [Fact]
    public async Task Patch_HR_ModifyingOther_AllowsAllFields()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        // Seed target employee
        db.Employees.Add(MakeEmployee(10, jobTitle: "Dev", gender: "M", marital: "S", birth: DateTime.UtcNow.AddYears(-20), hire: DateTime.UtcNow.AddYears(-2)));
        await db.SaveChangesAsync();


        A.CallTo(() => mapper.Map<EmployeeDTO>(A<object>._))
            .ReturnsLazily(call =>
            {
                var e = call.GetArgument<object>(0) as Employee ?? new Employee();

                return new EmployeeDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    JobTitle = e.JobTitle,
                    Gender = e.Gender,
                    MaritalStatus = e.MaritalStatus,
                    BirthDate = e.BirthDate,
                    HireDate = e.HireDate
                };
            });


        var controller = new EmployeeController(db, mapper);
        // HR modifying another person's profile
        SetUser(controller, RoleNames.HR, beId: 99);

        var dto = new EmployeeDTO
        {
            BusinessEntityID = 10,
            JobTitle = "Lead Dev",
            Gender = "F",
            MaritalStatus = "M",
            BirthDate = DateTime.UtcNow.AddYears(-30),
            HireDate = DateTime.UtcNow.AddYears(-1)
        };

        var result = await controller.Patch(10, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<EmployeeDTO>(ok.Value);

        Assert.Equal("Lead Dev", returned.JobTitle);
        Assert.Equal("F", returned.Gender);
        Assert.Equal("M", returned.MaritalStatus);
    }

    [Fact]
    public async Task Patch_Employee_ModifyingOther_ReturnsForbid()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        db.Employees.Add(MakeEmployee(1));
        db.Employees.Add(MakeEmployee(2));
        await db.SaveChangesAsync();

        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.Employee, beId: 1);

        var dto = new EmployeeDTO { BusinessEntityID = 2, Gender = "F" };

        var result = await controller.Patch(2, dto);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Patch_Employee_ModifyingSelf_OnlyGenderAndMarital()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        db.Employees.Add(MakeEmployee(5, jobTitle: "Dev", gender: "M", marital: "S"));
        await db.SaveChangesAsync();


        A.CallTo(() => mapper.Map<EmployeeDTO>(A<object>._))
            .ReturnsLazily(call =>
            {
                var e = call.GetArgument<object>(0) as Employee ?? new Employee();
                return new EmployeeDTO
                {
                    BusinessEntityID = e.BusinessEntityID,
                    JobTitle = e.JobTitle,
                    Gender = e.Gender,
                    MaritalStatus = e.MaritalStatus,
                    BirthDate = e.BirthDate,
                    HireDate = e.HireDate
                };
            });


        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.Employee, beId: 5);

        var dto = new EmployeeDTO
        {
            BusinessEntityID = 5,
            Gender = "F",
            MaritalStatus = "M",
            JobTitle = "TryChange" // should be ignored for self
        };

        var result = await controller.Patch(5, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<EmployeeDTO>(ok.Value);

        // gender/marital update applied
        Assert.Equal("F", returned.Gender);
        Assert.Equal("M", returned.MaritalStatus);
        // job title should remain original
        Assert.Equal("Dev", returned.JobTitle);
    }

    [Fact]
    public async Task Patch_ReturnsBadRequest_WhenIdMismatch()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        db.Employees.Add(MakeEmployee(7));
        await db.SaveChangesAsync();

        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.HR, 100);

        var dto = new EmployeeDTO { BusinessEntityID = 8, Gender = "F" }; // mismatch

        var result = await controller.Patch(7, dto);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Patch_ReturnsBadRequest_WhenDatesOutOfRange()
    {
        await using var db = BuildContext();
        var mapper = FakeMapper();

        db.Employees.Add(MakeEmployee(11));
        await db.SaveChangesAsync();

        var controller = new EmployeeController(db, mapper);
        SetUser(controller, RoleNames.HR, 100);

        var dto = new EmployeeDTO
        {
            BusinessEntityID = 11,
            HireDate = DateTime.MaxValue // invalid
        };

        var result = await controller.Patch(11, dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("HireDate is out of range.", bad.Value);
    }
}
