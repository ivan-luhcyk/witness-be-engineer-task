# Witness Backend Engineer Task

Local implementation with asynchronous lease parsing via Azure Functions and Redis caching.

## Services

- `HmlrApi`: mock/source API for schedules (`/schedules`).
- `LeaseApi`: public API (`GET /{titleNumber}`, `GET /results`).
- `LeaseProcessing.Functions`: secure parse worker (`POST /api/parse`).
- `Redis`: cache and coordination store.
- `Azurite`: local Azure Storage emulator (required by Functions host).

## API Endpoints

- `GET /{titleNumber}`
  - `200 OK`: parsed result exists in Redis.
  - `202 Accepted`: result not ready yet; parse was queued/in progress.
  - `500`: parse failed for this title (status marked as failed).
- `GET /results`
  - Returns all parsed lease results currently stored in Redis.

Example:

```powershell
curl http://localhost:8080/TGL24029
curl http://localhost:8080/results
```

## Processing Flow

1. Client calls `LeaseApi` `GET /{titleNumber}`.
2. API checks `lease:result:{TITLE}` in Redis.
3. If not found, API sets `lease:status:{TITLE}=Pending` and returns `202 Accepted`.
4. API attempts to acquire `lease:parse:lock` (short TTL) to avoid trigger spam.
5. If lock acquired, API signs an HMAC request and calls `LeaseProcessing` `POST /api/parse`.
6. Function verifies timestamp + nonce + signature, then fetches full list from `HmlrApi`.
7. Function parses all entries and stores:
   - result: `lease:result:{TITLE}`
   - status: `lease:status:{TITLE}=Completed`
8. If requested title is missing in parsed data, status for that title becomes `Failed`.

## Security

- API endpoint rate-limited by client IP (`FixedWindow`, 30 req/min).
- Function trigger protected by HMAC request signing:
  - `X-Request-Timestamp`
  - `X-Request-Nonce`
  - `X-Request-Signature`
- Replay protection: nonce is stored in Redis (`SET NX` + TTL).
- Shared secret token configured through env vars.

## Shared Library

`WitnessBackendEngineerTask.Common` contains:

- shared models:
  - `ParseRequest`
  - `ParsedScheduleNoticeOfLease`
  - `LeaseProcessingStatus`
- shared options:
  - `RedisOptions`
  - `RedisRetryOptions`
- shared resilience utility:
  - `IRetryRunner` / `RetryRunner`

## Run Locally

From repository root:

```powershell
docker compose build --no-cache
docker compose up
```

Exposed ports:

- Lease API: `http://localhost:8080`
- HMLR API: `http://localhost:8081`
- Redis: `localhost:6379`
- Azurite Blob/Queue/Table: `10000/10001/10002`

## Key Environment Variables (Docker Compose)

- Lease API:
  - `Parser__BaseUrl=http://lease-processing`
  - `Parser__TriggerPath=/api/parse`
  - `Parser__ServiceToken=...`
- Lease Processing Function:
  - `ParserAuth__ServiceToken=...` (must match `Parser__ServiceToken`)
  - `Hmlr__BaseUrl=http://hmlr-api:8080`
  - `AzureWebJobsStorage=...` (Azurite connection)
- Both:
  - `Redis__ConnectionString=redis:6379`

## Troubleshooting

- Error: `Connection refused (lease-processing:80)` from `LeaseApi`
  - Meaning: API cannot reach Function container on internal Docker network.
  - Check:
    1. `docker compose ps` and ensure `lease-processing` is `Up`.
    2. Function logs for startup errors: `docker compose logs lease-processing`.
    3. `Parser__BaseUrl` is `http://lease-processing` and trigger path is `/api/parse`.
    4. `Parser__ServiceToken` matches `ParserAuth__ServiceToken`.

- Function starts but no work happens
  - Confirm logs show function metadata loaded and parse endpoint active.
  - Confirm API logs show successful trigger call (no `EnsureSuccessStatusCode` exception).

- Azure host lock / container creation 404s in Azurite logs at startup
  - Expected in first boot cycle; host then creates containers and acquires lock.

## Design Notes

- `GET /{titleNumber}` is intentionally non-blocking; parsing runs asynchronously.
- Parse operation processes the full HMLR list in one run and writes all found titles to cache.
- Redis is used for:
  - result cache
  - per-title processing status
  - anti-spam lock + nonce replay protection

## More Detail

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for class-level documentation and sequence details.
