namespace BotiApp.Services.Sii;

public class SiiEmisorOptions
{
    public const string SectionName = "Sii:Emisor";

    public string? Rut { get; set; }
    public string? RazonSocial { get; set; }
    public string? Giro { get; set; }
    public string? Direccion { get; set; }
    public string? Comuna { get; set; }
    public string? Ambiente { get; set; }
}
