namespace Greenwaytech.PiholeApiClient.Model;



public record PiholeConfigModel
{
    public Dns dns { get; set; }
    public Dhcp dhcp { get; set; }
    public Ntp ntp { get; set; }
    public Resolver resolver { get; set; }
    public Database database { get; set; }
    public Webserver webserver { get; set; }
    public Files files { get; set; }
    public Misc misc { get; set; }
    public Debug debug { get; set; }
}

public record Dns
{
    public string[] upstreams { get; set; }
    public bool CNAMEdeepInspect { get; set; }
    public bool blockESNI { get; set; }
    public bool EDNS0ECS { get; set; }
    public bool ignoreLocalhost { get; set; }
    public bool showDNSSEC { get; set; }
    public bool analyzeOnlyAandAAAA { get; set; }
    public string piholePTR { get; set; }
    public string replyWhenBusy { get; set; }
    public int blockTTL { get; set; }
    public string[] hosts { get; set; }
    public bool domainNeeded { get; set; }
    public bool expandHosts { get; set; }
    public string domain { get; set; }
    public bool bogusPriv { get; set; }
    public bool dnssec { get; set; }
    public string _interface { get; set; }
    public string hostRecord { get; set; }
    public string listeningMode { get; set; }
    public bool queryLogging { get; set; }
    public object[] cnameRecords { get; set; }
    public int port { get; set; }
    public object[] revServers { get; set; }
    public Cache cache { get; set; }
    public Blocking blocking { get; set; }
    public Specialdomains specialDomains { get; set; }
    public Reply reply { get; set; }
    public Ratelimit rateLimit { get; set; }
}

public record Cache
{
    public int size { get; set; }
    public int optimizer { get; set; }
    public int upstreamBlockedTTL { get; set; }
}

public record Blocking
{
    public bool active { get; set; }
    public string mode { get; set; }
    public string edns { get; set; }
}

public record Specialdomains
{
    public bool mozillaCanary { get; set; }
    public bool iCloudPrivateRelay { get; set; }
    public bool designatedResolver { get; set; }
}

public record Reply
{
    public Host host { get; set; }
    public Blocking1 blocking { get; set; }
}

public record Host
{
    public bool force4 { get; set; }
    public string IPv4 { get; set; }
    public bool force6 { get; set; }
    public string IPv6 { get; set; }
}

public record Blocking1
{
    public bool force4 { get; set; }
    public string IPv4 { get; set; }
    public bool force6 { get; set; }
    public string IPv6 { get; set; }
}

public record Ratelimit
{
    public int count { get; set; }
    public int interval { get; set; }
}

public record Dhcp
{
    public bool active { get; set; }
    public string start { get; set; }
    public string end { get; set; }
    public string router { get; set; }
    public string netmask { get; set; }
    public string leaseTime { get; set; }
    public bool ipv6 { get; set; }
    public bool rapidCommit { get; set; }
    public bool multiDNS { get; set; }
    public bool logging { get; set; }
    public bool ignoreUnknownClients { get; set; }
    public object[] hosts { get; set; }
}

public record Ntp
{
    public Ipv4 ipv4 { get; set; }
    public Ipv6 ipv6 { get; set; }
    public Sync sync { get; set; }
}

public record Ipv4
{
    public bool active { get; set; }
    public string address { get; set; }
}

public record Ipv6
{
    public bool active { get; set; }
    public string address { get; set; }
}

public record Sync
{
    public bool active { get; set; }
    public string server { get; set; }
    public int interval { get; set; }
    public int count { get; set; }
    public Rtc rtc { get; set; }
}

public record Rtc
{
    public bool set { get; set; }
    public string device { get; set; }
    public bool utc { get; set; }
}

public record Resolver
{
    public bool resolveIPv4 { get; set; }
    public bool resolveIPv6 { get; set; }
    public bool networkNames { get; set; }
    public string refreshNames { get; set; }
}

public record Database
{
    public bool DBimport { get; set; }
    public int maxDBdays { get; set; }
    public int DBinterval { get; set; }
    public bool useWAL { get; set; }
    public Network network { get; set; }
}

public record Network
{
    public bool parseARPcache { get; set; }
    public int expire { get; set; }
}

public record Webserver
{
    public string domain { get; set; }
    public string acl { get; set; }
    public string port { get; set; }
    public int threads { get; set; }
    public string[] headers { get; set; }
    public bool serve_all { get; set; }
    public Session session { get; set; }
    public Tls tls { get; set; }
    public Paths paths { get; set; }
    public Interface _interface { get; set; }
    public Api api { get; set; }
}

public record Session
{
    public int timeout { get; set; }
    public bool restore { get; set; }
}

public record Tls
{
    public string cert { get; set; }
}

public record Paths
{
    public string webroot { get; set; }
    public string webhome { get; set; }
    public string prefix { get; set; }
}

public record Interface
{
    public bool boxed { get; set; }
    public string theme { get; set; }
}

public record Api
{
    public int max_sessions { get; set; }
    public bool prettyJSON { get; set; }
    public string pwhash { get; set; }
    public string password { get; set; }
    public string totp_secret { get; set; }
    public string app_pwhash { get; set; }
    public bool app_sudo { get; set; }
    public bool cli_pw { get; set; }
    public object[] excludeClients { get; set; }
    public object[] excludeDomains { get; set; }
    public int maxHistory { get; set; }
    public int maxClients { get; set; }
    public bool client_history_global_max { get; set; }
    public bool allow_destructive { get; set; }
    public Temp temp { get; set; }
}

public record Temp
{
    public int limit { get; set; }
    public string unit { get; set; }
}

public record Files
{
    public string pid { get; set; }
    public string database { get; set; }
    public string gravity { get; set; }
    public string gravity_tmp { get; set; }
    public string macvendor { get; set; }
    public string setupVars { get; set; }
    public string pcap { get; set; }
    public Log log { get; set; }
}

public record Log
{
    public string ftl { get; set; }
    public string dnsmasq { get; set; }
    public string webserver { get; set; }
}

public record Misc
{
    public int privacylevel { get; set; }
    public int delay_startup { get; set; }
    public int nice { get; set; }
    public bool addr2line { get; set; }
    public bool etc_dnsmasq_d { get; set; }
    public object[] dnsmasq_lines { get; set; }
    public bool extraLogging { get; set; }
    public bool readOnly { get; set; }
    public Check check { get; set; }
}

public record Check
{
    public bool load { get; set; }
    public int shmem { get; set; }
    public int disk { get; set; }
}

public record Debug
{
    public bool database { get; set; }
    public bool networking { get; set; }
    public bool locks { get; set; }
    public bool queries { get; set; }
    public bool flags { get; set; }
    public bool shmem { get; set; }
    public bool gc { get; set; }
    public bool arp { get; set; }
    public bool regex { get; set; }
    public bool api { get; set; }
    public bool tls { get; set; }
    public bool overtime { get; set; }
    public bool status { get; set; }
    public bool caps { get; set; }
    public bool dnssec { get; set; }
    public bool vectors { get; set; }
    public bool resolver { get; set; }
    public bool edns0 { get; set; }
    public bool clients { get; set; }
    public bool aliasclients { get; set; }
    public bool events { get; set; }
    public bool helper { get; set; }
    public bool config { get; set; }
    public bool inotify { get; set; }
    public bool webserver { get; set; }
    public bool extra { get; set; }
    public bool reserved { get; set; }
    public bool ntp { get; set; }
    public bool netlink { get; set; }
    public bool all { get; set; }
}

