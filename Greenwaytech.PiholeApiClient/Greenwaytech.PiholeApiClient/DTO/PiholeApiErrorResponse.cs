namespace Greenwaytech.PiholeApiClient.DTO;

public record PiholeApiErrorResponse
{
    public Error error { get; set; }
    public float took { get; set; }
}

public class Error
{
    public string key { get; set; }
    public string message { get; set; }
    public object hint { get; set; }
}

