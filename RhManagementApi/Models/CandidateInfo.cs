namespace RhManagementApi.Models;

public partial class CandidateInfo
{
    public int ID { get; set; }

    public int JobCandidateID { get; set; }

    public int OpeningID { get; set; }

    public string JobTitle { get; set; } = null!;

    public string NationalID { get; set; } = null!;

    public DateOnly BirthDate { get; set; }

    public string MaritalStatus { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string FirstName {get;set;} = null!;

    public string MiddleName {get;set;} = null!;

    public string LastName {get;set;} = null!;

    public string Email {get;set;} = null!;

    public string PhoneNumber {get;set;} = null!;

    public string Comment {get;set;} = null!;

    public virtual JobCandidate JobCandidate { get; set; } = null!;

    public virtual Opening Opening { get; set; } = null!;
}
