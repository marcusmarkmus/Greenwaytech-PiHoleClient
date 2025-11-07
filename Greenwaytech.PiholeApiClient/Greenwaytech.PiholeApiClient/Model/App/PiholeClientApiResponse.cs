namespace Greenwaytech.PiholeApiClient.Model.App;
public class PiholeClientApiResponse<T>
{
    public T? Data { get; set; }
    public required bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
