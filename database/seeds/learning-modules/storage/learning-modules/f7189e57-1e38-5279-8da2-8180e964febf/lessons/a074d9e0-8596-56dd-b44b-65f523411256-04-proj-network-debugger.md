# Optional Network Debugging Notes

## Purpose
This optional lesson provides a repeatable process for diagnosing browser networking problems. The goal is not to memorize every error code. The goal is to collect enough evidence to identify which layer failed.

## Learning outcomes
By the end of this lesson, you should be able to:

- Separate DNS, connection, TLS, HTTP, browser-policy, and application failures.
- Inspect requests in the browser Network panel.
- Use a small set of command-line tools to verify assumptions.
- Record debugging evidence without exposing credentials or personal data.
- Produce a concise network-incident note that another developer can reproduce.

## Start with the observed symptom
Write down exactly what failed before changing anything.

Useful details include:

- The full URL, with secrets removed.
- The time of the failure.
- Browser and operating system.
- Whether the failure occurs on another browser, device, account, or network.
- Whether every request fails or only one endpoint fails.
- Whether the failure is consistent or intermittent.
- The visible error message and HTTP status, when available.

Avoid vague descriptions such as "the API is broken." A useful symptom is:

```text
POST /api/orders returns 403 in production for signed-in users, while GET /api/orders returns 200.
```

## Classify the failing layer
Use the earliest failed stage to guide the investigation.

### 1. URL or client construction
Possible signs:

- Incorrect scheme, host, path, port, or query string.
- Request never appears in the Network panel.
- JavaScript error occurs before the request is created.

Check the generated URL and calling code.

### 2. DNS resolution
Possible signs:

- Host-not-found or name-resolution error.
- `nslookup` or `dig` returns no expected record.
- One domain fails while direct access to another host works.

Check domain spelling, records, name-server delegation, local overrides, and cache state.

### 3. Connection and routing
Possible signs:

- Connection refused.
- Connection timed out.
- Network unreachable.
- Correct DNS answer but no successful connection.

Check the port, service availability, firewall rules, proxy settings, and whether the server is listening on the expected interface.

### 4. TLS and certificate validation
Possible signs:

- Certificate warning.
- Name mismatch.
- Expired certificate.
- Trust-chain failure.
- TLS negotiation error.

Inspect the certificate served by the actual endpoint. Do not assume the certificate file on disk is the certificate currently being served.

### 5. HTTP response
Possible signs:

- A response appears with a `4xx` or `5xx` status.
- Redirect loop.
- Unexpected content type.
- Incorrect cache behavior.

Inspect status, headers, response body, redirects, and request method.

### 6. Browser policy
Possible signs:

- CORS error.
- Mixed-content block.
- Content Security Policy violation.
- Cookie blocked by browser policy.

Read the Console message and inspect the relevant response headers. Do not disable browser security as the production fix.

### 7. Application logic or data
Possible signs:

- HTTP request succeeds but returned data is wrong.
- UI does not update after a successful response.
- Validation error is expected but not handled.
- Response schema differs from the frontend assumption.

Inspect the response payload and then trace application state and rendering logic.

## Browser Network panel workflow
1. Open developer tools before reproducing the issue.
2. Select the **Network** panel.
3. Enable preservation of the log when redirects or page navigation matter.
4. Clear old requests.
5. Reproduce the problem once.
6. Select the failed request.
7. Inspect:
   - Request URL
   - Method
   - Status
   - Initiator
   - Request headers
   - Request body or payload
   - Response headers
   - Response body
   - Timing information
   - Redirect chain
8. Check the Console for policy or JavaScript errors.

Do not post an unredacted network export publicly. It may contain authorization headers, cookies, personal data, internal URLs, or request bodies.

## Reading timing information
Network timing views may show stages such as:

- Queueing or stalled time.
- DNS lookup.
- Initial connection.
- TLS negotiation.
- Request upload.
- Waiting for the first response byte.
- Content download.

Interpret the longest stage:

- Long DNS time suggests resolver or naming issues.
- Long connection time suggests routing, firewall, service, or distance issues.
- Long TLS time suggests connection setup or handshake problems.
- Long wait time after sending the request suggests server or upstream processing.
- Long download time suggests a large response or limited throughput.

A single trace is not enough to prove a general performance pattern. Repeat tests and compare similar conditions.

## Useful command-line checks
### Inspect response headers

```bash
curl -I https://example.com
```

### Show request and connection details

```bash
curl -v https://example.com
```

Verbose output may include sensitive headers. Redact it before sharing.

### Follow redirects

```bash
curl -L -I http://example.com
```

### Request a JSON endpoint

```bash
curl -i \
  -H "Accept: application/json" \
  https://example.com/api/status
```

### Inspect DNS on Windows

```powershell
nslookup example.com
```

### Inspect DNS where `dig` is available

```bash
dig example.com A
dig example.com AAAA
```

### Trace the network path
On Windows:

```powershell
tracert example.com
```

On macOS or Linux:

```bash
traceroute example.com
```

Routers may intentionally ignore trace or ping traffic. A missing reply does not by itself prove that normal HTTPS traffic is blocked.

## Common cases
### `404 Not Found`
Check:

- Host and base URL.
- Path spelling and case.
- API version prefix.
- Reverse-proxy route rules.
- Whether a frontend route fallback is hiding a missing API route.

### `401 Unauthorized`
Check:

- Whether credentials were sent.
- Token expiration.
- Cookie scope and browser cookie policy.
- Authentication scheme and header format.

### `403 Forbidden`
Check:

- User permissions.
- Server authorization rules.
- CSRF protection.
- Origin restrictions.
- WAF or security-gateway rules.

### `429 Too Many Requests`
Check:

- Retry behavior.
- Request loops.
- Rate-limit headers.
- Shared limits across users or environments.

Use server-provided retry guidance when available. Do not create an aggressive retry loop.

### `500 Internal Server Error`
Check:

- Server logs with the request time or correlation identifier.
- Input that triggered the failure.
- Recent deployments.
- Database or upstream-service errors.

The browser cannot reveal the full server-side cause unless the server includes safe diagnostic information.

### `502`, `503`, or `504`
Check the proxy, load balancer, application health, upstream address, startup state, and timeout configuration.

### CORS error
Check:

- Page origin.
- Request URL and method.
- Preflight `OPTIONS` request.
- `Origin` request header.
- `Access-Control-Allow-Origin` response header.
- Allowed methods and headers.
- Credential mode.

CORS is enforced by browsers. A command-line request may succeed while browser JavaScript is blocked.

### Request succeeds but UI fails
Check:

- Response content type.
- JSON parse errors.
- Response schema.
- Empty or partial data.
- State update logic.
- Rendering conditions.

A `200` response proves only that the HTTP exchange succeeded according to the server. It does not prove the application handled the result correctly.

## Minimal incident-note template
Use this structure:

```markdown
# Network issue

## Symptom
Exact failure and affected operation.

## Environment
Browser, operating system, deployment, account type, and network.

## Reproduction
1. Step one
2. Step two
3. Step three

## Expected result
What should happen.

## Actual result
What happens, including status and visible error.

## Request evidence
- Method:
- URL:
- Status:
- Response content type:
- Correlation ID:
- Relevant timing:

## Layer assessment
URL, DNS, connection, TLS, HTTP, browser policy, or application.

## Sensitive data removed
Headers, cookies, tokens, personal data, and internal values were redacted.

## Next action
The single most useful next check.
```

## Practice task
Choose a failed request from a local or test application, or deliberately request a missing path.

1. Capture the request in the Network panel.
2. Classify the failing layer.
3. Record the method, URL, status, response type, and timing.
4. Run one relevant command-line check.
5. Write an incident note using the template above.
6. Remove or replace every token, cookie, password, personal value, or internal address before sharing it.

## Completion evidence
You can complete this optional lesson when another developer can read your incident note, reproduce the issue, understand which network layer likely failed, and continue the investigation without asking for basic missing evidence.
