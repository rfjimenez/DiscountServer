DiscountServer - .NET 8 WebSocket Discount Code Server
======================================================

Overview
--------
DiscountServer is a .NET 8 server application for generating and using unique discount codes. It uses the WebSocket protocol for fast, bidirectional, real-time communication. Discount codes are stored persistently in a file, surviving service restarts.

Features
--------
- Generate random, non-repeating discount codes (7-8 characters).
- Use discount codes (each code can only be used once).
- Persistent file-based storage (`Storage/discount_codes.json`).
- Automatically creates the `Storage` folder if it is missing.
- Handles multiple requests in parallel.
- WebSocket protocol for communication.
- Unit tests for core logic.

Getting Started
---------------
Prerequisites:
- .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

Setup:
1. Clone the repository:
   git clone https://github.com/rfjimenez/DiscountServer.git
   cd DiscountServer

3. Restore dependencies:
   dotnet restore

4. Build the project:
   dotnet build

5. Run the server:
   dotnet run --project DiscountServer

   The server will start and listen for WebSocket connections.
   Default WebSocket URL: ws://localhost:5253/ws or wss://localhost:7277/ws
   (Check DiscountServer/Properties/launchSettings.json for the actual port and path.)

Testing with a Simple WebSocket Client
--------------------------------------
You can use websocat (https://github.com/vi/websocat), Postman, browser, or any WebSocket client.

Example using websocat:
   websocat ws://localhost:5253/ws

Generate codes request:
   Send text: GENERATE|5|8
   - 5: Number of codes
   - 8: Code length

Use code request:
   Send text: USE|ABC12345
   - ABC12345: Discount code to use

Responses
---------
- For GENERATE, server replies with:
    - `"true"` (as a text message) if codes were successfully generated.
    - A single byte with value `3` (InvalidRequest) if the request was malformed or failed.
- For USE, server replies with a result code as a byte value.

USE Response Codes
------------------
When you send a USE request, the server responds with a single byte value:
- `0` = Success (code was valid and is now marked as used)
- `1` = AlreadyUsed (code was already used)
- `2` = NotFound (code does not exist)
- `3` = InvalidRequest (request was malformed)

Why WebSockets?
---------------
- Real-time, bidirectional communication.
- Lower overhead than HTTP APIs.
- Simple protocol, easy to implement and test.
- Ideal for exam scenarios requiring interactive, parallel requests.

How It Works
------------
- Server listens for WebSocket connections.
- Client sends requests in the format:
    GENERATE|count|length
    USE|code
- Server generates unique codes, saves them to disk (`Storage/discount_codes.json`), and marks codes as used.
- If the `Storage` folder is missing, it is automatically created before saving codes.
- Handles invalid or malformed requests gracefully.
- Responses are sent back according to the protocol.

Design Decisions
----------------
- WebSocket chosen for simplicity and real-time capabilities.
- File-based storage for persistence and easy setup.
- Automatic folder creation for robust persistence.
- Thread-safe code generation and usage for parallel requests.
- Unit tests ensure reliability of core logic.
