using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole;

public record PiholeConfigModel
{
    [JsonPropertyName("dns")]
    public Dns? Dns { get; set; }
    [JsonPropertyName("dhcp")]
    public Dhcp? Dhcp { get; set; }
    [JsonPropertyName("ntp")]
    public Ntp? Ntp { get; set; }
    [JsonPropertyName("resolver")]
    public Resolver? Resolver { get; set; }
    [JsonPropertyName("database")]
    public Database? Database { get; set; }
    [JsonPropertyName("webserver")]
    public Webserver? Webserver { get; set; }
    [JsonPropertyName("files")]
    public Files? Files { get; set; }
    [JsonPropertyName("misc")]
    public Misc? Misc { get; set; }
    [JsonPropertyName("debug")]
    public Debug? Debug { get; set; }
}

public record Dns
{
    [JsonPropertyName("upstreams")]
    public string[]? Upstreams { get; set; }
    [JsonPropertyName("CNAMEdeepInspect")]
    public bool? CNAMEdeepInspect { get; set; }
    [JsonPropertyName("blockESNI")]
    public bool? BlockESNI { get; set; }
    [JsonPropertyName("EDNS0ECS")]
    public bool? EDNS0ECS { get; set; }
    [JsonPropertyName("ignoreLocalhost")]
    public bool? IgnoreLocalhost { get; set; }
    [JsonPropertyName("showDNSSEC")]
    public bool? ShowDNSSEC { get; set; }
    [JsonPropertyName("analyzeOnlyAandAAAA")]
    public bool? AnalyzeOnlyAandAAAA { get; set; }
    [JsonPropertyName("piholePTR")]
    public string? PiholePTR { get; set; }
    [JsonPropertyName("replyWhenBusy")]
    public string? ReplyWhenBusy { get; set; }
    [JsonPropertyName("blockTTL")]
    public int? BlockTTL { get; set; }
    [JsonPropertyName("hosts")]
    public string[]? Hosts { get; set; }
    [JsonPropertyName("domainNeeded")]
    public bool? DomainNeeded { get; set; }
    [JsonPropertyName("expandHosts")]
    public bool? ExpandHosts { get; set; }
    [JsonPropertyName("domain")]
    public DnsDomain? Domain { get; set; }
    [JsonPropertyName("bogusPriv")]
    public bool? BogusPriv { get; set; }
    [JsonPropertyName("dnssec")]
    public bool? Dnssec { get; set; }
    [JsonPropertyName("_interface")]
    public string? Interface { get; set; }
    [JsonPropertyName("hostRecord")]
    public string? HostRecord { get; set; }
    [JsonPropertyName("listeningMode")]
    public string? ListeningMode { get; set; }
    [JsonPropertyName("queryLogging")]
    public bool? QueryLogging { get; set; }
    [JsonPropertyName("cnameRecords")]
    public object[]? CnameRecords { get; set; }
    [JsonPropertyName("port")]
    public int? Port { get; set; }
    [JsonPropertyName("revServers")]
    public object[]? RevServers { get; set; }
    [JsonPropertyName("cache")]
    public Cache? Cache { get; set; }
    [JsonPropertyName("blocking")]
    public Blocking? Blocking { get; set; }
    [JsonPropertyName("specialDomains")]
    public Specialdomains? SpecialDomains { get; set; }
    [JsonPropertyName("reply")]
    public Reply? Reply { get; set; }
    [JsonPropertyName("rateLimit")]
    public Ratelimit? RateLimit { get; set; }
}

/// <summary>
/// This was historically just a string, but is now an object with more details.
/// Update your pihole if you see issues deserializing!
/// </summary>
public record DnsDomain
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("local")]
    public bool? Local { get; set; }
}

public record Cache
{
    [JsonPropertyName("size")]
    public int? Size { get; set; }
    [JsonPropertyName("optimizer")]
    public int? Optimizer { get; set; }
    [JsonPropertyName("upstreamBlockedTTL")]
    public int? UpstreamBlockedTTL { get; set; }
}

public record Blocking
{
    [JsonPropertyName("active")]
    public bool? Active { get; set; }
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }
    [JsonPropertyName("edns")]
    public string? Edns { get; set; }
}

public record Specialdomains
{
    [JsonPropertyName("mozillaCanary")]
    public bool? MozillaCanary { get; set; }
    [JsonPropertyName("iCloudPrivateRelay")]
    public bool? ICloudPrivateRelay { get; set; }
    [JsonPropertyName("designatedResolver")]
    public bool? DesignatedResolver { get; set; }
}

public record Reply
{
    [JsonPropertyName("host")]
    public Host? Host { get; set; }
    [JsonPropertyName("blocking")]
    public Blocking1? Blocking { get; set; }
}

public record Host
{
    [JsonPropertyName("force4")]
    public bool? Force4 { get; set; }
    [JsonPropertyName("IPv4")]
    public string? IPv4 { get; set; }
    [JsonPropertyName("force6")]
    public bool? Force6 { get; set; }
    [JsonPropertyName("IPv6")]
    public string? IPv6 { get; set; }
}

public record Blocking1
{
    [JsonPropertyName("force4")]
    public bool? Force4 { get; set; }
    [JsonPropertyName("IPv4")]
    public string? IPv4 { get; set; }
    [JsonPropertyName("force6")]
    public bool? Force6 { get; set; }
    [JsonPropertyName("IPv6")]
    public string? IPv6 { get; set; }
}

public record Ratelimit
{
    [JsonPropertyName("count")]
    public int? Count { get; set; }
    [JsonPropertyName("interval")]
    public int? Interval { get; set; }
}

public record Dhcp
{
    [JsonPropertyName("active")]
    public bool? Active { get; set; }
    [JsonPropertyName("start")]
    public string? Start { get; set; }
    [JsonPropertyName("end")]
    public string? End { get; set; }
    [JsonPropertyName("router")]
    public string? Router { get; set; }
    [JsonPropertyName("netmask")]
    public string? Netmask { get; set; }
    [JsonPropertyName("leaseTime")]
    public string? LeaseTime { get; set; }
    [JsonPropertyName("ipv6")]
    public bool? Ipv6 { get; set; }
    [JsonPropertyName("rapidCommit")]
    public bool? RapidCommit { get; set; }
    [JsonPropertyName("multiDNS")]
    public bool? MultiDNS { get; set; }
    [JsonPropertyName("logging")]
    public bool? Logging { get; set; }
    [JsonPropertyName("ignoreUnknownClients")]
    public bool? IgnoreUnknownClients { get; set; }
    [JsonPropertyName("hosts")]
    public object[]? Hosts { get; set; }
}

public record Ntp
{
    [JsonPropertyName("ipv4")]
    public Ipv4? Ipv4 { get; set; }
    [JsonPropertyName("ipv6")]
    public Ipv6? Ipv6 { get; set; }
    [JsonPropertyName("sync")]
    public Sync? Sync { get; set; }
}

public record Ipv4
{
    [JsonPropertyName("active")]
    public bool? Active { get; set; }
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public record Ipv6
{
    [JsonPropertyName("active")]
    public bool? Active { get; set; }
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public record Sync
{
    [JsonPropertyName("active")]
    public bool? Active { get; set; }
    [JsonPropertyName("server")]
    public string? Server { get; set; }
    [JsonPropertyName("interval")]
    public int? Interval { get; set; }
    [JsonPropertyName("count")]
    public int? Count { get; set; }
    [JsonPropertyName("rtc")]
    public Rtc? Rtc { get; set; }
}

public record Rtc
{
    [JsonPropertyName("set")]
    public bool? Set { get; set; }
    [JsonPropertyName("device")]
    public string? Device { get; set; }
    [JsonPropertyName("utc")]
    public bool? Utc { get; set; }
}

public record Resolver
{
    [JsonPropertyName("resolveIPv4")]
    public bool? ResolveIPv4 { get; set; }
    [JsonPropertyName("resolveIPv6")]
    public bool? ResolveIPv6 { get; set; }
    [JsonPropertyName("networkNames")]
    public bool? NetworkNames { get; set; }
    [JsonPropertyName("refreshNames")]
    public string? RefreshNames { get; set; }
}

public record Database
{
    [JsonPropertyName("DBimport")]
    public bool? DBimport { get; set; }
    [JsonPropertyName("maxDBdays")]
    public int? MaxDBdays { get; set; }
    [JsonPropertyName("DBinterval")]
    public int? DBinterval { get; set; }
    [JsonPropertyName("useWAL")]
    public bool? UseWAL { get; set; }
    [JsonPropertyName("network")]
    public Network? Network { get; set; }
}

public record Network
{
    [JsonPropertyName("parseARPcache")]
    public bool? ParseARPcache { get; set; }
    [JsonPropertyName("expire")]
    public int? Expire { get; set; }
}

public record Webserver
{
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }
    [JsonPropertyName("acl")]
    public string? Acl { get; set; }
    [JsonPropertyName("port")]
    public string? Port { get; set; }
    [JsonPropertyName("threads")]
    public int? Threads { get; set; }
    [JsonPropertyName("headers")]
    public string[]? Headers { get; set; }
    [JsonPropertyName("serve_all")]
    public bool? Serve_all { get; set; }
    [JsonPropertyName("session")]
    public Session? Session { get; set; }
    [JsonPropertyName("tls")]
    public Tls? Tls { get; set; }
    [JsonPropertyName("paths")]
    public Paths? Paths { get; set; }
    [JsonPropertyName("_interface")]
    public Interface? Interface { get; set; }
    [JsonPropertyName("api")]
    public Api? Api { get; set; }
}

public record Session
{
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }
    [JsonPropertyName("restore")]
    public bool? Restore { get; set; }
}

public record Tls
{
    [JsonPropertyName("cert")]
    public string? Cert { get; set; }
}

public record Paths
{
    [JsonPropertyName("webroot")]
    public string? Webroot { get; set; }
    [JsonPropertyName("webhome")]
    public string? Webhome { get; set; }
    [JsonPropertyName("prefix")]
    public string? Prefix { get; set; }
}

public record Interface
{
    [JsonPropertyName("boxed")]
    public bool? Boxed { get; set; }
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
}

public record Api
{
    [JsonPropertyName("max_sessions")]
    public int? Max_sessions { get; set; }
    [JsonPropertyName("prettyJSON")]
    public bool? PrettyJSON { get; set; }
    [JsonPropertyName("pwhash")]
    public string? Pwhash { get; set; }
    [JsonPropertyName("password")]
    public string? Password { get; set; }
    [JsonPropertyName("totp_secret")]
    public string? Totp_secret { get; set; }
    [JsonPropertyName("app_pwhash")]
    public string? App_pwhash { get; set; }
    [JsonPropertyName("app_sudo")]
    public bool? App_sudo { get; set; }
    [JsonPropertyName("cli_pw")]
    public bool? Cli_pw { get; set; }
    [JsonPropertyName("excludeClients")]
    public object[]? ExcludeClients { get; set; }
    [JsonPropertyName("excludeDomains")]
    public object[]? ExcludeDomains { get; set; }
    [JsonPropertyName("maxHistory")]
    public int? MaxHistory { get; set; }
    [JsonPropertyName("maxClients")]
    public int? MaxClients { get; set; }
    [JsonPropertyName("client_history_global_max")]
    public bool? Client_history_global_max { get; set; }
    [JsonPropertyName("allow_destructive")]
    public bool? Allow_destructive { get; set; }
    [JsonPropertyName("temp")]
    public Temp? Temp { get; set; }
}

public record Temp
{
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
}

public record Files
{
    [JsonPropertyName("pid")]
    public string? Pid { get; set; }
    [JsonPropertyName("database")]
    public string? Database { get; set; }
    [JsonPropertyName("gravity")]
    public string? Gravity { get; set; }
    [JsonPropertyName("gravity_tmp")]
    public string? Gravity_tmp { get; set; }
    [JsonPropertyName("macvendor")]
    public string? Macvendor { get; set; }
    [JsonPropertyName("setupVars")]
    public string? SetupVars { get; set; }
    [JsonPropertyName("pcap")]
    public string? Pcap { get; set; }
    [JsonPropertyName("log")]
    public Log? Log { get; set; }
}

public record Log
{
    [JsonPropertyName("ftl")]
    public string? Ftl { get; set; }
    [JsonPropertyName("dnsmasq")]
    public string? Dnsmasq { get; set; }
    [JsonPropertyName("webserver")]
    public string? Webserver { get; set; }
}

public record Misc
{
    [JsonPropertyName("privacylevel")]
    public int? Privacylevel { get; set; }
    [JsonPropertyName("delay_startup")]
    public int? Delay_startup { get; set; }
    [JsonPropertyName("nice")]
    public int? Nice { get; set; }
    [JsonPropertyName("addr2line")]
    public bool? Addr2line { get; set; }
    [JsonPropertyName("etc_dnsmasq_d")]
    public bool? Etc_dnsmasq_d { get; set; }
    [JsonPropertyName("dnsmasq_lines")]
    public object[]? Dnsmasq_lines { get; set; }
    [JsonPropertyName("extraLogging")]
    public bool? ExtraLogging { get; set; }
    [JsonPropertyName("readOnly")]
    public bool? ReadOnly { get; set; }
    [JsonPropertyName("check")]
    public Check? Check { get; set; }
}

public record Check
{
    [JsonPropertyName("load")]
    public bool? Load { get; set; }
    [JsonPropertyName("shmem")]
    public int? Shmem { get; set; }
    [JsonPropertyName("disk")]
    public int? Disk { get; set; }
}

public record Debug
{
    [JsonPropertyName("database")]
    public bool? Database { get; set; }
    [JsonPropertyName("networking")]
    public bool? Networking { get; set; }
    [JsonPropertyName("locks")]
    public bool? Locks { get; set; }
    [JsonPropertyName("queries")]
    public bool? Queries { get; set; }
    [JsonPropertyName("flags")]
    public bool? Flags { get; set; }
    [JsonPropertyName("shmem")]
    public bool? Shmem { get; set; }
    [JsonPropertyName("gc")]
    public bool? Gc { get; set; }
    [JsonPropertyName("arp")]
    public bool? Arp { get; set; }
    [JsonPropertyName("regex")]
    public bool? Regex { get; set; }
    [JsonPropertyName("api")]
    public bool? Api { get; set; }
    [JsonPropertyName("tls")]
    public bool? Tls { get; set; }
    [JsonPropertyName("overtime")]
    public bool? Overtime { get; set; }
    [JsonPropertyName("status")]
    public bool? Status { get; set; }
    [JsonPropertyName("caps")]
    public bool? Caps { get; set; }
    [JsonPropertyName("dnssec")]
    public bool? Dnssec { get; set; }
    [JsonPropertyName("vectors")]
    public bool? Vectors { get; set; }
    [JsonPropertyName("resolver")]
    public bool? Resolver { get; set; }
    [JsonPropertyName("edns0")]
    public bool? Edns0 { get; set; }
    [JsonPropertyName("clients")]
    public bool? Clients { get; set; }
    [JsonPropertyName("aliasclients")]
    public bool? Aliasclients { get; set; }
    [JsonPropertyName("events")]
    public bool? Events { get; set; }
    [JsonPropertyName("helper")]
    public bool? Helper { get; set; }
    [JsonPropertyName("config")]
    public bool? Config { get; set; }
    [JsonPropertyName("inotify")]
    public bool? Inotify { get; set; }
    [JsonPropertyName("webserver")]
    public bool? Webserver { get; set; }
    [JsonPropertyName("extra")]
    public bool? Extra { get; set; }
    [JsonPropertyName("reserved")]
    public bool? Reserved { get; set; }
    [JsonPropertyName("ntp")]
    public bool? Ntp { get; set; }
    [JsonPropertyName("netlink")]
    public bool? Netlink { get; set; }
    [JsonPropertyName("all")]
    public bool? All { get; set; }
}

