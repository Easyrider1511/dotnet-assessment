# .NET Software Engineering Assessment

Five independent .NET solutions, each in its own folder, unified under a single solution file (`Assessment.slnx`).

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (Q4 only) [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) — local Azure Storage emulator
  ```
  npm install -g azurite
  azurite --silent --location ./azurite-data
  ```

---

## Build all projects

```bash
dotnet build Assessment.slnx
```

## Run all tests

```bash
dotnet test Assessment.slnx
```

---

## Q1 — Palindrome Checker

**Project:** `Q1_Palindrome/` · **Tests:** `Q1_Palindrome.Tests/`

Checks whether a word, phrase, or number is a palindrome. Case is ignored. Non-alphanumeric characters (punctuation, whitespace, special characters) are stripped before comparison, so asymmetrical whitespace and punctuation have no effect.

### Run

```bash
dotnet run --project Q1_Palindrome
```

```
=== Palindrome Checker ===
Input: Deleveled
  "Deleveled" IS a palindrome.

Input: A man a plan a canal Panama
  "A man a plan a canal Panama" IS a palindrome.
```

### Test coverage — `PalindromeChecker` class

| Line | Branch | Method |
|------|--------|--------|
| 100% | 100% | 100% |

> `Program.cs` (interactive console loop) is excluded from coverage measurement.

---

## Q2 — Quicksort

**Project:** `Q2_Quicksort/` · **Tests:** `Q2_Quicksort.Tests/`

Recursive quicksort for up to 10 integer or floating-point elements, read from standard input. Supports duplicate values.

### Run

```bash
dotnet run --project Q2_Quicksort
```

```
=== Quicksort ===
Number of elements to sort (1-10): 5
Element 1: 3
Element 2: 1.5
Element 3: 3
Element 4: 9
Element 5: 2

Original order: 3, 1.5, 3, 9, 2
Sorted order:   1.5, 2, 3, 3, 9
```

### Test coverage — `QuickSorter` class

| Line | Branch | Method |
|------|--------|--------|
| 100% | 100% | 100% |

---

## Q3 — Age Calculator (ASP.NET MVC)

**Project:** `Q3_AgeCalculator/` · **Tests:** `Q3_AgeCalculator.Tests/`

Web application that accepts a date of birth and displays the user's age broken down into years, months, weeks, days, hours, minutes, and seconds.

### Run

```bash
dotnet run --project Q3_AgeCalculator
```

Then open `https://localhost:5001` (or the port shown in the console).

### Test coverage — `AgeResult` model

| Line | Branch | Method |
|------|--------|--------|
| 100% | 100% | 100% |

---

## Q4 — File Service (ASP.NET Web API + Azure Blob Storage)

**Project:** `Q4_FileService/` · **Tests:** `Q4_FileService.Tests/`

REST API that uploads files to Azure Blob Storage and records metadata (name, size, content type, extension, location, timestamp, blob path) in a SQLite database for historical querying.

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/files/upload` | Upload a file (multipart/form-data). Fields: `file`, `location` |
| `GET`  | `/api/files` | List all uploaded files, newest first |
| `GET`  | `/api/files/{id}` | Get a single record by ID |
| `GET`  | `/api/files/history?from=&to=&location=` | Query by date range and/or location |

### Run locally (Azurite emulator)

1. Start Azurite: `azurite --silent --location ./azurite-data`
2. Run the API: `dotnet run --project Q4_FileService`
3. Browse OpenAPI docs at `https://localhost:5001/openapi/v1.json`

### Switching to a real Azure Blob Store

1. In the [Azure Portal](https://portal.azure.com), create a **Storage Account**.
2. Go to **Storage Account → Security + networking → Access keys**.
3. Copy the **Connection string** for key1 or key2.
4. Replace the value in `Q4_FileService/appsettings.json`:
   ```json
   "AzureBlobStorage": "DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net"
   ```
   Or set it via an environment variable (recommended for production):
   ```bash
   ConnectionStrings__AzureBlobStorage="<connection-string>"
   ```

### Test coverage — `FilesController` class

| Line | Branch | Method |
|------|--------|--------|
| 100% | 100% | 100% |

---

## Q5 — ANPR Camera File Processor (.NET Worker Service)

**Project:** `Q5_ANPR/` · **Tests:** `Q5_ANPR.Tests/`

A background service that monitors one or more camera folders for `*.lpr` files produced by ACS ANPR cameras, parses them, and stores the plate reads in a SQLite database. No file is processed more than once, even if different cameras use the same filename.

### LPR file format

```
NONE\r9112A\r77\rGIBEXIT2\20140827\1210/w27082014,12140198,9112A,77.jpg
```

| Field | Example | Notes |
|-------|---------|-------|
| CountryOfVehicle | `NONE` | Country of origin |
| RegNumber | `r9112A` → `9112A` | Leading `r` dropped |
| ConfidenceLevel | `r77` → `77` | Leading `r` dropped |
| CameraName | `rGIBEXIT2` → `GIBEXIT2` | Leading `r` dropped |
| Date | `20140827` | Format `yyyyMMdd` |
| Time | `1210` | Format `HHmm` (24-hour) |
| ImageFilename | `w27082014,...` | Leading `w` dropped |

### Database schema

```sql
CREATE TABLE PlateReads (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    CountryOfVehicle TEXT    NOT NULL,
    RegNumber        TEXT    NOT NULL,
    ConfidenceLevel  INTEGER NOT NULL,
    CameraName       TEXT    NOT NULL,
    CapturedAt       TEXT    NOT NULL,
    ImageFilename    TEXT    NOT NULL,
    SourceFileKey    TEXT    NOT NULL UNIQUE  -- full normalised path; cameras sharing a filename stay distinct
);

CREATE INDEX IX_PlateReads_CameraName_CapturedAt ON PlateReads (CameraName, CapturedAt);
```

### Configuration (`Q5_ANPR/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=anpr.db"
  },
  "CameraFolders": [
    "C:\\ANPR\\Camera1",
    "C:\\ANPR\\Camera2"
  ]
}
```

### Run

```bash
dotnet run --project Q5_ANPR
```

The service immediately processes any `*.lpr` files already present and then watches the configured folders for new ones.

### Test coverage — `LprParser` and `PlateReadRepository`

| Class | Line | Branch | Method |
|-------|------|--------|--------|
| `LprParser` | 100% | 100% | 100% |
| `PlateReadRepository` | 100% | 100% | 100% |

> The `Worker` background service and `Program.cs` bootstrap are excluded from unit test coverage — they require integration testing with a real host.

---

## Test summary

| Project | Tests | Result |
|---------|-------|--------|
| Q1_Palindrome.Tests | 67 | Passed |
| Q2_Quicksort.Tests | 15 | Passed |
| Q3_AgeCalculator.Tests | 17 | Passed |
| Q4_FileService.Tests | 14 | Passed |
| Q5_ANPR.Tests | 23 | Passed |
| **Total** | **136** | **0 failures** |
