using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Greenwaytech.PiholeApiClient.Authentication;
internal class PiholeAuthHandler : DelegatingHandler

{
    private readonly ILogger<PiholeAuthHandler> _logger;
    private readonly IPiholeSessionProvider _sessionProvider;

    public PiholeAuthHandler(ILogger<PiholeAuthHandler> logger, IPiholeSessionProvider sessionProvider)
    {
        _logger = logger;
        _sessionProvider = sessionProvider;
    }

    /// <summary>
    /// Overload for non-DI contexts that uses a console logger by default.
    /// </summary>
    public PiholeAuthHandler(IPiholeSessionProvider sessionProvider)
        : this(new Greenwaytech.PiholeApiClient.Logging.ConsoleLogger<PiholeAuthHandler>(), sessionProvider)
    {
    }

    [ActivatorUtilitiesConstructor]
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = await _sessionProvider.GetValidSessionAsync();
        request.Headers.Add("sid", session.Sid);
        return await base.SendAsync(request, cancellationToken);
    }
}

