using Microsoft.Extensions.Logging;

namespace Greenwaytech.PiholeApiClient.Authentication;
public class PiholeAuthHandler : DelegatingHandler

{
    private readonly ILogger<PiholeAuthHandler> _logger;
    private readonly IPiholeSessionProvider _sessionProvider;

    public PiholeAuthHandler(ILogger<PiholeAuthHandler> logger, IPiholeSessionProvider sessionProvider)
    {
        _logger = logger;
        _sessionProvider = sessionProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = await _sessionProvider.GetValidSessionAsync();
        request.Headers.Add("sid", session.Sid);
        return await base.SendAsync(request, cancellationToken);
    }
}

