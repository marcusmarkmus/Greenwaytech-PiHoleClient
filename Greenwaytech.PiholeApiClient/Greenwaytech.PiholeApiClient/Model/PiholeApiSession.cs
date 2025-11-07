namespace Greenwaytech.PiholeApiClient.Model;

public record PiholeApiSession
{
    public bool valid { get; set; }
    public bool totp { get; set; }
    public string sid { get; set; }
    public string csrf { get; set; }
    public int validity { get; set; }
    public string message { get; set; }
    public required DateTimeOffset PiholeAuthResponseTimeStamp { get; set; }

    public bool IsValid() 
        => valid && !string.IsNullOrEmpty(sid) && validity > 0 && PiholeAuthResponseTimeStamp.AddSeconds(validity) > DateTimeOffset.UtcNow;
}

