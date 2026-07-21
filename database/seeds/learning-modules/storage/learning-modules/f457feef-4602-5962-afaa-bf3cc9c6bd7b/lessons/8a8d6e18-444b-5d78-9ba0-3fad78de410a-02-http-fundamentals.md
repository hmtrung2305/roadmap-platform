# HTTP Fundamentals

## Learning outcomes
By the end of this lesson, you should be able to:

- Describe the structure of an HTTP request and response.
- Choose an appropriate HTTP method for a common operation.
- Interpret major status-code groups.
- Explain the roles of headers, bodies, content types, and caching.
- Inspect HTTP traffic with browser developer tools or `curl`.

## What HTTP does
**HTTP**, or Hypertext Transfer Protocol, defines how clients and servers exchange application messages on the Web.

HTTP uses a request-response model:

1. A client sends a request.
2. A server processes it.
3. The server returns a response.

HTTP is an application-layer protocol. It does not itself provide encryption. HTTPS is HTTP carried over a secure TLS connection.

## Anatomy of a request
A request contains:

- A **method**, such as `GET` or `POST`.
- A **target**, usually a path and optional query string.
- **Headers** containing metadata.
- An optional **body** containing data.

A simplified request looks like this:

```http
GET /api/products?category=books HTTP/1.1
Host: example.com
Accept: application/json
User-Agent: ExampleBrowser/1.0
```

A request that sends JSON may look like this:

```http
POST /api/products HTTP/1.1
Host: example.com
Content-Type: application/json
Accept: application/json

{
  "name": "Notebook",
  "price": 12.5
}
```

The blank line separates the headers from the body.

## Common HTTP methods
### GET
Retrieves a resource or representation.

```http
GET /api/products/42
```

A `GET` request should not be used to perform an action that changes server state.

### POST
Submits data for processing or creates a subordinate resource.

```http
POST /api/products
```

Repeated identical `POST` requests may create repeated results, so clients should be careful when retrying them.

### PUT
Creates or fully replaces a resource at a known location.

```http
PUT /api/products/42
```

### PATCH
Partially updates a resource.

```http
PATCH /api/products/42
```

### DELETE
Requests removal of a resource.

```http
DELETE /api/products/42
```

The method communicates intent, but the server still controls the actual behavior.

## Safe and idempotent methods
A method is **safe** when it is intended only to retrieve information. `GET` and `HEAD` are safe methods.

A method is **idempotent** when repeating the same request should have the same intended effect as sending it once. `GET`, `PUT`, and `DELETE` are defined as idempotent. `POST` is not generally idempotent.

Idempotency matters for retries. A network failure may leave the client uncertain about whether the server completed a request.

## Anatomy of a response
A response contains:

- A **status code**.
- A reason phrase in HTTP/1.x representations.
- Response **headers**.
- An optional **body**.

Example:

```http
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: max-age=60

{
  "id": 42,
  "name": "Notebook",
  "price": 12.5
}
```

## Status-code groups
### 1xx: Informational
The request is still being processed or another protocol-level action is occurring.

### 2xx: Successful
The server accepted and processed the request.

Common examples:

- `200 OK`: successful response.
- `201 Created`: a resource was created.
- `204 No Content`: successful response with no response body.

### 3xx: Redirection
The client must use another location or a cached representation may still be valid.

Common examples:

- `301 Moved Permanently`: permanent redirect.
- `302 Found`: temporary redirect in common web usage.
- `304 Not Modified`: cached content can be reused.

### 4xx: Client error
The request is invalid, unauthorized, forbidden, or refers to a missing resource.

Common examples:

- `400 Bad Request`: invalid request data or syntax.
- `401 Unauthorized`: authentication is required or failed.
- `403 Forbidden`: the server understood the request but refuses it.
- `404 Not Found`: the resource was not found.
- `409 Conflict`: the request conflicts with current server state.
- `422 Unprocessable Content`: the syntax may be valid, but the supplied data cannot be processed.
- `429 Too Many Requests`: a rate limit was exceeded.

### 5xx: Server error
The server failed while handling a valid or potentially valid request.

Common examples:

- `500 Internal Server Error`: an unexpected server failure.
- `502 Bad Gateway`: an upstream server returned an invalid response.
- `503 Service Unavailable`: the service is temporarily unavailable.
- `504 Gateway Timeout`: an upstream server did not respond in time.

A status code is only one part of the response. APIs often include a structured error body with additional details.

## Important headers
### Content-Type
Describes the media type of the body.

```http
Content-Type: application/json
```

Other examples include `text/html`, `text/css`, `image/png`, and `application/pdf`.

### Accept
Tells the server which response formats the client can process.

```http
Accept: application/json
```

### Authorization
Carries credentials or access tokens.

```http
Authorization: Bearer <token>
```

Sensitive credentials must not be exposed in logs, screenshots, or client-side source code.

### Cache-Control
Defines caching rules.

```http
Cache-Control: public, max-age=3600
```

### Cookie and Set-Cookie
`Set-Cookie` asks the browser to store a cookie. `Cookie` sends matching stored cookies back to the server on later requests.

### Location
Often identifies the destination of a redirect or the URL of a newly created resource.

## Query parameters, path parameters, and bodies
These mechanisms serve different purposes:

- **Path parameters** identify a resource: `/products/42`.
- **Query parameters** modify retrieval, filtering, sorting, or pagination: `/products?category=books&page=2`.
- **Request bodies** send structured data for creation or updates.

Do not place secrets in query parameters. URLs may be stored in browser history, logs, analytics systems, or referrer information.

## HTTP is stateless
Each HTTP request can be understood independently at the protocol level. Applications add continuity through mechanisms such as cookies, session identifiers, access tokens, and stored client state.

Stateless does not mean the server stores no data. It means the protocol does not automatically remember application context between requests.

## Basic caching
Caching avoids transferring or generating the same representation unnecessarily.

A server can provide freshness rules:

```http
Cache-Control: max-age=300
```

After the response becomes stale, a client can validate it with an identifier such as an `ETag`:

```http
If-None-Match: "product-42-v7"
```

If the representation has not changed, the server can respond with `304 Not Modified` and omit the full body.

Caching improves performance, but incorrect caching can display stale or private data. Cache behavior must match the resource.

## Practice task
Use a public page or a local application.

1. Open the browser's **Network** panel.
2. Reload the page.
3. Find one document request and one API, script, image, or stylesheet request.
4. For each request, record:
   - Method
   - URL
   - Status code
   - Request headers
   - Response headers
   - Content type
   - Response size
5. Explain why the selected method and status code are appropriate.

You can also inspect a URL from a terminal:

```bash
curl -I https://example.com
```

The `-I` option requests response headers without downloading the normal response body.

## Knowledge check
1. What is the difference between a request header and a request body?
2. When should `PATCH` be preferred over `PUT`?
3. Why is `401` different from `403`?
4. Why can retrying a `POST` request be risky?
5. What does `304 Not Modified` allow the client to do?

## Completion evidence
You can complete this lesson when you can read a request and response in developer tools, explain the selected method and status code, and identify the headers that control content type, authentication, or caching.
