namespace InwardDC.Domain.Entities;

/// <summary>Vendor master. Source of purchase inward entries.</summary>
public class Vendor : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
    public string GSTIN { get; set; } = string.Empty;
    public string PAN { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
