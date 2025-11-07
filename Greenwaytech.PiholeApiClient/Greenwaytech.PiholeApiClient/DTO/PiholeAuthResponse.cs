namespace Greenwaytech.PiholeApiClient.DTO;

public record PiholeAuthResponse
{
    public AuthSession session { get; init; }
    public float took { get; init; }
}

public record AuthSession
{
    public bool valid { get; init; }
    public bool totp { get; init; }
    public string sid { get; init; }
    public string csrf { get; init; }
    public int validity { get; init; }
    public string message { get; init; }
}
