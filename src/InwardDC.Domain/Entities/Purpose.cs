namespace InwardDC.Domain.Entities;

/// <summary>
/// Purpose of an inward / dispatch document (e.g., Evaluation, Testing, Demo,
/// Service, Other). Managed as a master list so the options can be added/edited.
/// </summary>
public class Purpose : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
