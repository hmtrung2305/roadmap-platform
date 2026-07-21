# Domain Names and DNS

## Learning outcomes
By the end of this lesson, you should be able to:

- Distinguish a domain name, host name, URL, and IP address.
- Explain how DNS resolution turns a name into an address.
- Recognize common DNS record types and their purposes.
- Describe how DNS caching and TTL values affect changes.
- Use basic tools to inspect DNS records.

## Why DNS exists
Computers route traffic using IP addresses, but people prefer readable names such as `example.com`. The **Domain Name System**, or DNS, is a distributed naming system that maps names to records.

DNS is often described as the Internet's phone book, but it does more than map names to addresses. It can also identify mail servers, delegate authority to name servers, publish verification values, and provide service-specific information.

## Domain names, host names, and URLs
Consider:

```text
https://docs.example.com:443/guides/dns?level=beginner#records
```

Its parts include:

- Scheme: `https`
- Host name: `docs.example.com`
- Port: `443`
- Path: `/guides/dns`
- Query string: `level=beginner`
- Fragment: `records`

`example.com` is the registered domain in this example. `docs` is a subdomain label. Together, `docs.example.com` is a host name.

A **URL** identifies the location and access method for a resource. A domain name is only one component of a URL.

## Domain hierarchy
DNS names are hierarchical and are read from right to left.

For `api.shop.example.com`:

- `com` is the top-level domain.
- `example.com` is the registered domain.
- `shop.example.com` is a subdomain.
- `api.shop.example.com` is a more specific host name.

The root of the DNS hierarchy is represented internally by a trailing dot, although users normally omit it.

## How DNS resolution works
A simplified lookup for `www.example.com` follows these steps:

1. The browser checks its own DNS-related caches.
2. The operating system checks its cache and local host configuration.
3. The device asks a configured **recursive resolver**.
4. If the resolver has no valid cached answer, it queries the DNS hierarchy.
5. A root name server points it toward the appropriate top-level-domain servers.
6. The top-level-domain server points it toward the domain's authoritative name servers.
7. An authoritative server returns the requested record.
8. The resolver caches the result and sends it back to the device.

In practice, cached answers often eliminate several of these steps.

## Common DNS records
### A
Maps a name to an IPv4 address.

```text
example.com.  A  192.0.2.10
```

### AAAA
Maps a name to an IPv6 address.

```text
example.com.  AAAA  2001:db8::10
```

### CNAME
Makes one name an alias of another name.

```text
www.example.com.  CNAME  example-host.example.net.
```

A CNAME points to another name, not directly to an IP address.

### MX
Identifies mail servers for a domain.

```text
example.com.  MX  10 mail.example.com.
```

The number is a priority value. Lower values are preferred.

### TXT
Stores text values. TXT records are commonly used for domain verification and email-related policies.

```text
example.com.  TXT  "verification=value"
```

### NS
Identifies the authoritative name servers for a DNS zone.

```text
example.com.  NS  ns1.example-dns.net.
```

### CAA
Restricts which certificate authorities may issue certificates for a domain.

### SRV
Describes the host and port for a named service.

Not every hosting provider exposes every record type, but the underlying concepts remain the same.

## Apex domains and subdomains
The **apex**, also called the zone root, is the domain without an added subdomain, such as `example.com`.

A host such as `www.example.com` is a subdomain. DNS providers may treat apex records differently from subdomain records because standard CNAME behavior is restricted at a zone apex where other required records must also exist.

Some providers offer flattened or provider-specific alias records to make the apex point to hosted infrastructure. The exact name of that feature varies by provider.

## TTL and caching
DNS records include a **time to live**, or TTL, which tells resolvers how long they may cache an answer.

Suppose a record has a TTL of 3600 seconds. A resolver that cached the old value may continue using it for up to the remaining cache duration after the record changes.

This is why DNS updates can appear at different times for different users. The delay is usually caused by caches expiring at different times, not by every server copying the change through a single global system.

Lowering a TTL before a planned migration can reduce the future cache window, but the lower value must itself be active before the migration.

## DNS does not redirect web pages
DNS selects an address or another name. It does not perform an HTTP redirect from one path to another.

For example, pointing `old.example.com` and `new.example.com` to the same server does not automatically redirect users. The web server or application must return a redirect response such as `301` or `308`.

## Common DNS problems
### Name does not resolve
Possible causes include:

- Missing record.
- Incorrect name-server delegation.
- Typographical error.
- Expired domain registration.
- DNSSEC configuration failure.

### Name resolves to the wrong server
Possible causes include:

- Stale cached records.
- Incorrect A, AAAA, CNAME, or provider alias value.
- A local hosts-file override.

### One network works and another does not
Different networks may use different resolvers with different cached data or filtering behavior.

### Domain works but HTTPS fails
DNS may be correct while the server certificate does not cover the host name, the server is misconfigured, or the TLS connection fails for another reason.

DNS resolution and HTTPS validation are separate stages.

## Inspecting DNS records
On Windows, you can use:

```powershell
nslookup example.com
nslookup -type=mx example.com
```

On systems with `dig`, you can use:

```bash
dig example.com A
dig example.com AAAA
dig example.com MX
dig example.com NS
```

Results may vary by resolver and cache state. Use an authoritative-server query when you need to compare the current authoritative value with a cached recursive answer.

## Practice task
Choose a domain you are allowed to inspect.

1. Find its A and AAAA records.
2. Find its authoritative NS records.
3. Check whether `www` uses an A, AAAA, or CNAME record.
4. Record the TTL shown by your tool, when available.
5. Open the site and identify the host name in the final URL after redirects.
6. Explain which behavior came from DNS and which behavior came from HTTP.

## Knowledge check
1. Why is a URL not the same thing as a domain name?
2. What is the difference between a recursive resolver and an authoritative name server?
3. Why can two users receive different DNS answers shortly after a change?
4. What does a CNAME record point to?
5. Why can correct DNS still result in an HTTPS error?

## Completion evidence
You can complete this lesson when you can break down a URL, explain the DNS resolution path, identify common record types, and inspect the records for a real domain using a command-line tool.
