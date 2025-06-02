# Witness Backend Engineer Task

## The Problem
We've provided the `HmlrApi` project. 
This project is a lightweight mock data provider you should treat as a third party API (HMLR).
For the sake of speed and ease, focus on consuming this api hosted locally.

This API has two routes:
1. `GET /schedules`
   Returns a collection of raw Schedule of Notice of Lease Data.  This is data provided in a lossy format.
1. `GET /results`
   The Expected Output of *your* API we expect to see.

Your goal is to deliver a C# .NET API that exposes a `GET /{titleNumber}` endpoint and serves fully parsed lease information. You’ll consume the existing `HmlrApi`’s raw Schedule data, offload parsing/transformation into a serverless compute (e.g. an Azure Function), and return results in the same schema as the `GET /results` endpoint. Think about how you’ll:

- Design a non-blocking request workflow (e.g. immediately returning a “processing” response if parsing isn’t yet complete, then serving the structured data once it’s ready).
- Implement caching or persistent storage so that once a title’s data has been transformed, subsequent calls to `GET /{titleNumber}` return instantly without re-parsing.
- Handle error states, concurrency, and any retries or timeouts.

Rather than following a strict step-by-step guide, we want to see your design and implementation choices—how you structure your API, how you invoke and manage the serverless function, how you optimize for performance and reliability, and how you document your approach. All final output must match the existing `GET /results` format (order doesn’t matter, accuracy does).

No modification to the provided code should be necessary.

# Run Instructions

## Dependencies
The API was built with .NET 8 Minimal API. As such you'll need the .NET 8 SDK and relevant dev tools.

## To run with VSStudio / Rider
1. Open the HmlrApi.csproj
1. Hit Debug.
1. Api will open and navigate to swagger. 

## To run with VSCode
1. Open your terminal.
1. Navigate to `/HmlrApi`
1. Run `dotnet run`
1. Navigate to `https://localhost:7203/swagger/index.html`
1. You may need to run `dotnet dev-certs https --trust` to accept the dotnet dev ssl cert

## Auth
The API uses Basic auth and the credentials are:
- `Username`: Username
- `Password`: Password

# What we're looking for

## Core Competencies
* Show off how you like to code and include the below:
   * Consistent standards
   * SOLID Principles
   * SoC
   * Code readability
   * Well documented code
   * Anything else you think is relevant, get creative!
* Simple to run or well documented startup.
* Accurate Result Set.
* Test Coverage.

## Time
We expect this to take no more than two hours to cover the core competencies above, if you decide to spend more time to show off or hit some bonus points you're more than welcome to!

If you find yourself running out of time prior to completing the entire problem, we will also review partial submissions paired with a write up of what your next steps would have been.

# Submission
Submit your result via your medium of choice (zip archive / git repo) to the recruiter or contact at Orbital, and we'll be in touch with the results! 
