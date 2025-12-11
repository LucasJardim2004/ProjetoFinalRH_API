namespace RhManagementApi.Models;

public partial class Opening
{
    public int OpeningID { get; set; }

    public string JobTitle { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly DateCreated { get; set; }

    public bool OpenFlag { get; set; }
}
