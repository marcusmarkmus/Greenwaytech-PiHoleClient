namespace Greenwaytech.PiholeApiClient.Model.Configuration;

public interface IPiHoleInstanceApiConfig
{
    string ApiBaseUrl { get; set; }
    string ApiKey { get; set; }

    bool Equals(object? obj);
    bool Equals(PiHoleInstanceApiConfig? other);
    int GetHashCode();
    string ToString();
}