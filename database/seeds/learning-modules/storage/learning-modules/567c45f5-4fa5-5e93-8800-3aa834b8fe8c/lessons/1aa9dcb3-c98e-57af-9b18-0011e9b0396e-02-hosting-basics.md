# Hosting Basics

## Learning outcomes
By the end of this lesson, you should be able to:

- Explain what a hosting platform provides.
- Distinguish static hosting from server-based application hosting.
- Describe the roles of origins, CDNs, domains, DNS, and TLS.
- Identify the configuration and operational concerns of a basic deployment.
- Select a reasonable hosting model for a small frontend project.

## What hosting means
**Hosting** makes application files and services available from infrastructure that users can reach over a network.

For a simple website, hosting may mean storing HTML, CSS, JavaScript, images, and fonts on a server. For a larger application, hosting may also include application processes, databases, background jobs, object storage, secrets, logs, and monitoring.

Hosting is not one single technology. It is a collection of infrastructure and operational responsibilities.

## Static hosting
A **static site** consists of files that can be served without running application logic for each request.

Typical files include:

```text
index.html
assets/app.js
assets/app.css
images/logo.png
```

A build tool may generate these files from source code. The host then serves the generated output.

Static hosting is suitable for:

- Documentation sites.
- Portfolios.
- Marketing pages.
- Client-rendered single-page applications.
- Sites generated ahead of time.

Advantages include simple deployment, strong cacheability, and a small server-side attack surface.

A static frontend can still communicate with external APIs. Static describes how the frontend files are hosted, not whether the overall product has dynamic behavior.

## Dynamic application hosting
A dynamic application executes server-side code when handling requests.

The server may:

- Authenticate users.
- Apply business rules.
- Query a database.
- Generate HTML.
- Return API responses.
- Process uploaded files.

Dynamic hosting requires a runtime such as .NET, Node.js, Java, Python, Go, or Rust, depending on the application.

It also introduces lifecycle concerns such as process health, scaling, database connections, secret management, and deployment compatibility.

## Origin servers and CDNs
The **origin** is the authoritative location from which content or application responses are served.

A **content delivery network**, or CDN, operates distributed edge locations that can cache or proxy content closer to users.

A common flow is:

```text
User -> CDN edge -> origin server
```

If the CDN has a valid cached copy, it may respond without contacting the origin. Otherwise, it forwards the request and may cache the result.

CDNs can improve latency, reduce origin load, and absorb traffic spikes. They do not remove the need to configure caching correctly.

## Domain, DNS, and TLS
A deployment normally needs three separate pieces to work together:

1. **Hosting target**: where the files or application run.
2. **DNS records**: how the domain points to that target.
3. **TLS certificate**: how HTTPS proves the server's identity and encrypts traffic.

A site can be successfully deployed but unreachable through its intended domain because DNS is wrong. It can also resolve correctly but fail HTTPS because the certificate or server configuration is wrong.

Treat these as related but distinct configuration layers.

## Deployment artifacts
A hosting platform usually expects a specific artifact.

For a frontend project, the artifact may be a generated directory such as:

```text
dist/
```

For a server application, the artifact may include compiled binaries, runtime files, container images, or packaged source code.

The deployment should be reproducible. A reliable process defines:

- The required runtime version.
- The build command.
- The output directory.
- Environment variables.
- Startup command, when applicable.
- Database migrations, when applicable.

Do not rely on manual edits made only on the server. Those changes are difficult to reproduce and can be lost during redeployment.

## Environments and configuration
Applications often use separate environments, such as:

- Local development.
- Testing or staging.
- Production.

Configuration may differ across environments. Examples include API base URLs, logging levels, database connections, and feature flags.

Sensitive values such as database passwords, API secrets, and private keys should be stored through the host's secret-management or environment-configuration mechanism. They should not be committed to the repository or embedded in frontend bundles.

Any value shipped to browser JavaScript must be treated as public, even when its name contains `SECRET` or `PRIVATE`.

## Single-page application routing
A client-rendered application may use routes such as:

```text
/dashboard
/settings/profile
```

When a user directly opens `/settings/profile`, the server may look for a physical file at that path and return `404`.

Static hosts often need a fallback rule that serves `index.html` for application routes. The client-side router then renders the correct view.

This fallback must not incorrectly replace real asset or API errors. Configure it according to the application's route structure.

## Caching and asset versioning
Static assets can be cached aggressively when their filenames change with their content.

Example:

```text
app.4f8a21c.js
```

When the file content changes, the build creates a new name. The old file can remain cached without hiding the new release.

The main HTML document is often cached more conservatively because it points to the current asset filenames.

Incorrect caching can cause a user to receive old HTML that references deleted files or a mix of incompatible release assets.

## Basic reliability concerns
Even a small deployment should consider:

- What happens if the build fails.
- Whether the previous release can be restored.
- How health is checked.
- Where logs are stored.
- How errors are detected.
- Whether backups exist for persistent data.
- Whether the service has resource or usage limits.

A successful deployment command does not prove that the application is usable. Verification should include opening the deployed application, testing core routes, and checking browser and server errors.

## Choosing a hosting model
Use **static hosting** when the deployable output is only files and all server-side capabilities are provided elsewhere.

Use **application hosting** when you must run server code, maintain long-lived processes, access private server-side credentials, or connect directly to protected infrastructure.

Use a **managed platform** when you want the provider to handle more infrastructure. Use lower-level infrastructure when the application requires control that a managed platform cannot provide. Greater control usually adds more operational responsibility.

## Practice task
Create a deployment plan for a small frontend application.

Include:

1. Hosting model: static or dynamic.
2. Build command.
3. Deployment artifact or output directory.
4. Required environment variables.
5. Domain and DNS configuration.
6. HTTPS setup.
7. Client-side route fallback, if needed.
8. Cache policy for HTML and hashed assets.
9. A three-step post-deployment smoke test.
10. A rollback approach.

The plan should be specific enough that another developer could deploy the project without guessing hidden steps.

## Knowledge check
1. Why can a static frontend still be part of a dynamic application?
2. What is the difference between an origin and a CDN edge?
3. Why must frontend environment values be treated as public?
4. Why can refreshing a client-side route produce a server `404`?
5. Why should deployment artifacts be generated through a reproducible build?

## Completion evidence
You can complete this lesson when you can choose a hosting model for a project, explain how the domain reaches the deployment, and produce a deployment plan that covers build, configuration, routing, caching, verification, and rollback.
