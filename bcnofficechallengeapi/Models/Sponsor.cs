namespace bcnofficechallengeapi.Models;

public class Sponsor
{
    public Guid Id { get; set; }

    /// <summary>
    /// Secret UUID embedded in the QR code. Never exposed in the public API.
    /// </summary>
    public Guid QrId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Url { get; set; }

    /// <summary>
    /// URL del logo del sponsor (almacenado en Azure Blob Storage u otro CDN).
    /// </summary>
    public string? ImageUrl { get; set; }

    public int PointsValue { get; set; }
}
