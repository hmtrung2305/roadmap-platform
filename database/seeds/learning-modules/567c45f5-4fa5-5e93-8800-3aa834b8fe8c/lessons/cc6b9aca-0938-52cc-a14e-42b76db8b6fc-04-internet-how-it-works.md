# How the Internet Works

## Learning outcomes
By the end of this lesson, you should be able to:

- Explain the difference between the Internet and the Web.
- Describe how data travels between a browser and a server.
- Identify the roles of IP addresses, routers, ports, and transport protocols.
- Trace the major steps that occur after entering a URL in a browser.

## The Internet and the Web are not the same thing
The **Internet** is the global system of connected computer networks. It provides the infrastructure that allows devices to exchange data.

The **Web** is one service that runs on top of the Internet. It uses technologies such as URLs, HTTP, HTML, CSS, and JavaScript to deliver websites and web applications.

Other Internet-based services include email, file transfer, voice calls, online games, and remote access. A browser usually interacts with the Web, but the browser still depends on the Internet underneath it.

## Networks, devices, and addresses
A device must be connected to a network before it can communicate with another device. Your computer may connect through Wi-Fi or Ethernet to a router. That router then connects through an Internet service provider to other networks.

Devices use **IP addresses** to identify where data should be delivered.

- **IPv4** addresses look like `192.0.2.10`.
- **IPv6** addresses look like `2001:db8::10`.

Humans usually use domain names such as `example.com` instead of remembering IP addresses. DNS translates those names into addresses. DNS is covered in more detail in the Domain Names and DNS lesson.

## Packets and routing
Data is not normally sent as one continuous block. It is divided into smaller units called **packets**.

Each packet contains information that helps the network deliver it, including source and destination addresses. Routers inspect this information and forward packets toward their destination. Different packets from the same request may take different routes, depending on network conditions.

The receiving device reassembles the data into the original message. If packets are lost, a reliable transport protocol can request that they be sent again.

## Transport protocols and ports
Applications communicate through transport protocols. The two most common are:

### TCP
**Transmission Control Protocol**, or TCP, provides ordered and reliable delivery. It establishes a connection, tracks sent data, retransmits missing data, and presents the receiver with the data in the correct order.

HTTP/1.1 and HTTP/2 commonly use TCP.

### UDP
**User Datagram Protocol**, or UDP, sends data without establishing the same type of reliable connection. It has less overhead but does not guarantee delivery or order by itself.

Some modern web traffic, including HTTP/3, uses QUIC, which runs over UDP and provides reliability and security at a different layer.

### Ports
An IP address identifies a device or network interface. A **port** identifies a service on that device.

Common examples include:

- Port `80` for HTTP.
- Port `443` for HTTPS.
- Port `22` for SSH.

A browser connecting to `https://example.com` normally contacts port `443` unless the URL specifies another port.

## What happens when you enter a URL
Consider this URL:

```text
https://www.example.com/products?id=42
```

A simplified request flow is:

1. **The browser parses the URL.** It identifies the scheme, host, path, and query string.
2. **DNS resolves the host.** The browser obtains an IP address for `www.example.com`.
3. **A network connection is created.** The browser connects to the server through TCP or QUIC.
4. **TLS is negotiated for HTTPS.** The browser verifies the server certificate and establishes encrypted communication.
5. **The browser sends an HTTP request.** The request includes a method, path, headers, and sometimes a body.
6. **The server processes the request.** It may read files, execute application logic, query a database, or contact another service.
7. **The server sends an HTTP response.** The response contains a status code, headers, and usually a body.
8. **The browser processes the response.** For HTML, it may request additional CSS, JavaScript, fonts, and images.
9. **The browser renders the page.** It builds internal structures, calculates layout, paints pixels, and responds to user interaction.

Each step can fail independently. A DNS problem, refused connection, expired certificate, server error, or malformed response will produce a different symptom.

## Client and server roles
A **client** initiates a request. A browser, mobile application, command-line program, or another server can act as a client.

A **server** listens for requests and returns responses. A single physical machine can run many server processes, and one application may use several servers behind a load balancer.

The terms describe roles during communication, not necessarily fixed types of hardware.

## Latency and bandwidth
Two network properties strongly affect user experience:

- **Latency** is the delay before data begins to arrive. It is influenced by physical distance, routing, connection setup, and server processing.
- **Bandwidth** is the amount of data that can be transferred over time.

A small request can still feel slow when latency is high. A large image or video can be slow when available bandwidth is limited. Frontend performance work must consider both.

## Practice task
Choose a page in your browser and open the browser's developer tools.

1. Open the **Network** panel.
2. Reload the page.
3. Select the main document request.
4. Record the following:
   - Request URL
   - Remote address, when shown
   - Request method
   - Status code
   - Protocol
   - Response content type
5. Write a short request path using this format:

```text
Browser -> DNS -> network connection -> TLS -> HTTP request -> server -> HTTP response -> browser rendering
```

For each step, add one sentence describing what it contributes.

## Knowledge check
1. What is the difference between the Internet and the Web?
2. Why are packets used instead of sending all data as one indivisible block?
3. What does an IP address identify, and what does a port identify?
4. Which steps occur before an HTTPS request can be sent?
5. Why can a page feel slow even when it transfers little data?

## Completion evidence
You can complete this lesson when you can explain the full browser-to-server request path, identify the purpose of IP addresses and ports, and use the Network panel to inspect one real request.
