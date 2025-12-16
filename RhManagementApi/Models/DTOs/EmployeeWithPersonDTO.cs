namespace RhManagementApi.DTOs
{
    public class EmployeeWithPersonDTO
    {
        // Person fields
        public string? PersonType { get; set; }  // default "EM"
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;

        // Employee DTO (your existing one) without BusinessEntityID
        public EmployeeDTO EmployeeDTO { get; set; } = default!;
    }

}