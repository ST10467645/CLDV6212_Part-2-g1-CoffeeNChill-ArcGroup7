# CoffeeNChill Canteen Management System

**Module:** CLDV6212 (Cloud Development B) | **Group 7 (ARC)** | **Current submission: Part 2 (Queue Triggers & Docker Compose)**

> **DELETE THIS BOX ON DAY 9.** Progress tracker (update a row when it is merged to `main`):
>
> | Part 2 item | Owner | Status |
> |---|---|---|
> | Models, `host.json` Base64, queue packages | Kandyce | Done |
> | `SubmitOrder` producer (validation + Base64) | Kandyce | Done |
> | Server-generated OrderId/timestamp, queue error handling | Kandyce | Done |
> | `GetOrders`, `GetOrderById` | Kandyce | Done |
> | `ProcessOrderQueue` trigger + Received row | Kristen | Done |
> | Status simulation (Preparing, Ready, Collected) | Kristen | Day 3 |
> | `UpdateOrderStatus`, poison handling | Kristen | Day 4 |
> | Compose file, network, volume | Thami | Done |
> | Docker Hub `v2.0` images + compose switch | Thami | Day 3 and Day 5 |
> | Postman Part 2 tests, video, PDF | All | Days 6–9 |

## Group Members
| Name | Student Number |
|---|---|
| Kandyce Smit | ST10467645 |
| Kristen Keve | ST10472683 |
| Thamsanqa Ncube | ST10482062 |

## Project Overview
CoffeeNChill is a campus canteen that struggles with queues during lecture breaks. Part 1 replaced the paper menu and filing cabinet with Azure Table Storage, Azure Blob Storage and HTTP-triggered Azure Functions running against the Azurite emulator (Microsoft Learn, 2022; Microsoft Learn, 2025a; Microsoft Learn, 2025b; Microsoft Learn, 2026e). Part 2 adds **asynchronous order processing**: a new order is validated and placed on an Azure Storage Queue, and a background function picks it up, saves it in an `Orders` table and tracks its status (Microsoft Learn, 2026g). Putting a queue between the customer and the kitchen means the customer gets an instant reply and orders are handled one by one instead of crashing the system at peak times (Mrzyglod and Camden, 2020). The whole stack starts with a single `docker compose up` using images pulled from Docker Hub (Docker, 2026f).

## Repository Note
Part 1 was built in a repository created through the university's GitHub Classroom, which is private, and was mirrored to a public repository for assessment. Part 2 follows the same process: the work is done in a new private Classroom repository and, at the end of the project, it is mirrored with its full commit history to a public repository for submission and assessment. The Part 1 project was migrated into the Part 2 repository. Each team member commits from their own GitHub account on their own branch (`kandyce-part2-producer`, `kristen-part2-processor` and `thami-part2-compose`) and merges through pull requests.

- Part 1 public repository: https://github.com/ST10467645/CLDV6212-g1-CoffeeNChill-ArcGroup7.git
- Part 2 private repository (working copy): https://github.com/EMGPSD/Group_1_CLDV6212_Part-2-_Group_7_on_ARC.git
- Part 2 public repository (submission): PUBLIC_REPO_LINK_HERE

## Team Members and Contributions
| Name | Student Number | Role | Part 1 | Part 2 |
|---|---|---|---|---|
| Kandyce Smit | ST10467645 | Menu, Order Producer & Documentation Lead | MenuItem model, 5 Menu functions, README | Order models, `host.json` Base64 setting, `SubmitOrder`, `GetOrders`, `GetOrderById`, Postman collection, README, submission document |
| Kristen Keve | ST10472683 | Staff Documents, Queue Trigger & Order Tracking | Upload, List, Download staff documents | `ProcessOrderQueue`, Orders table writes, status lifecycle, `UpdateOrderStatus`, poison-queue handling |
| Thamsanqa Ncube | ST10482062 | Docker, Docker Hub & Compose | Dockerfile, Docker Hub `v1.0` images, Azurite container | `docker-compose.yml`, bridge network, Azurite volume, `v2.0` images, compose guide |

## Changelog
### Part 2
- **Added** `POST /api/orders/queue`: validates an order and places it on `order-processing-queue` as a Base64-encoded JSON message; replies 202 Accepted.
- **Added** `ProcessOrderQueue`: queue-triggered function that saves each order to the `Orders` table and advances its status `Received` → `Preparing` → `Ready` → `Collected`.
- **Added** `GET /api/orders`, `GET /api/orders/{orderDate}/{orderId}` and `PUT /api/orders/{orderDate}/{orderId}/status`.
- **Added** poison-queue handling (`order-processing-queue-poison`).
- **Added** `docker-compose.yml` (Azurite + Functions, custom bridge network, persistent Azurite volume) using Docker Hub `v2.0` images.
- **Changed** `host.json`: queue `messageEncoding` set to `base64` and `maxPollingInterval` set to 2 seconds.
- **Changed** Postman collections extended with queue, Orders, status, poison and edge-case tests.
- **Changed** code-comment attributions standardised to Harvard referencing.
- **Removed** the unused default `Function1` template.
- **Kept** all Part 1 endpoints unchanged.
### Part 1
- Menu CRUD (Table Storage), staff documents (Blob Storage), standalone Dockerfile, Docker Hub `v1.0` images, Postman collection.

## Architecture
```
Client / Postman
      │  POST /api/orders/queue
      ▼
[SubmitOrder]  validate → JSON → Base64
      ▼
[ order-processing-queue ]   (Azurite Queue, port 10001)
      │  message arrives
      ▼
[ProcessOrderQueue]  [QueueTrigger]
      │  saves and updates row
      ▼
[ Orders table ]   (Azurite Table, port 10002)  ← GET /api/orders, PUT .../status
      │  fails repeatedly
      ▼
[ order-processing-queue-poison ]

docker compose up → "azurite" container + "functions" container on network "coffeenchill-net"
```

## Prerequisites (Tools to Install)
- Docker Desktop
- Postman
- Git
- Visual Studio 2022 (Azure development workload) and the .NET 9 SDK, only needed to run from source
- Azure Storage Explorer (optional, to view queues and tables)

## Quick Start (single command)
```text
git clone PUBLIC_REPO_LINK_HERE
cd FOLDER_NAME_CREATED_BY_THE_CLONE
docker compose up
```
1. Wait until the Functions container logs show the host has started. A 404 from Azurite in the first seconds is normal.
2. The API is at `http://localhost:7071/api`.
3. Stop with `Ctrl+C`, then run `docker compose down` (add `-v` only to delete the stored Azurite data).
4. If ports are busy, run `docker ps -a` and remove old CoffeeNChill containers with `docker stop` and `docker rm`.

Compose pulls `st10482062/coffeenchill-functions:v2.0` and `st10482062/coffeenchill-azurite:v2.0` from Docker Hub and does not build locally (Docker, 2026f).

## Running From Source (optional)
1. Start Azurite in Docker:
```text
docker run -d --name coffeenchill-azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite azurite --skipApiVersionCheck --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0
```
2. Make sure a `local.settings.json` file is in the project root:
```text
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```
3. Open the solution in Visual Studio and press F5.

## Project Structure
```text
CoffeeNChillFunctions Part 2/          (repository root)
├── Functions/
│   ├── CreateMenuItem.cs
│   ├── DeleteMenuItem.cs
│   ├── DownloadStaffDocument.cs
│   ├── GetAllMenuItems.cs
│   ├── GetMenuItemsByCategory.cs
│   ├── GetOrderById.cs
│   ├── GetOrders.cs
│   ├── ListStaffDocuments.cs
│   ├── ProcessOrderQueue.cs
│   ├── SubmitOrder.cs
│   ├── UpdateMenuItem.cs
│   ├── UpdateOrderStatus.cs
│   └── UploadStaffDocument.cs
├── Models/
│   ├── MenuItem.cs
│   ├── Order.cs
│   ├── OrderEntity.cs
│   ├── OrderResponse.cs
│   └── OrderStatus.cs
├── Properties/
├── docs/
│   ├── docker-screenshots/
│   ├── Final Submission Document/
│   ├── Postman Collection/
│   ├── screenshots_part2/
│   │   ├── Kandyce/
│   │   ├── kristen/
│   │   └── Thami/
│   └── staff-docs_TestDocuments/
├── .gitignore
├── CoffeeNChillFunctions.csproj
├── CoffeeNChillFunctions.slnx
├── Dockerfile
├── docker-commands.md
├── docker-compose.yml
├── host.json
├── local.settings.json
├── Program.cs
└── README.md
```
`bin/` and `obj/` are build output and are excluded from version control through `.gitignore`.

## Docker Commands
See `docker-commands.md` in the repository root (standalone and Compose commands) and `docs/docker-compose-guide.md`.

## Docker Hub Images
Images are built, tagged and pushed to public Docker Hub repositories (Docker, 2026a; Docker, 2026b).
- Functions: https://hub.docker.com/r/st10482062/coffeenchill-functions (`v1.0`, `v2.0`)
- Azurite: https://hub.docker.com/r/st10482062/coffeenchill-azurite (`v1.0`, `v2.0`)

## Compose Design Choices
| Choice | Reason |
|---|---|
| `image:` with `:v2.0` tags, no `build:` | Compose pulls exactly the published version from Docker Hub (Docker, 2026f) |
| Custom bridge network `coffeenchill-net` | Containers reach each other by service name (`azurite`) instead of `localhost` (Docker, 2026e) |
| Named volume `azurite-data` | Queue and table data survive `docker compose down` (Docker, 2026g) |
| `restart: unless-stopped` | Functions retry if Azurite is not ready yet |

## API Endpoints
### Orders (Azure Storage Queue + Table Storage), new in Part 2
| Method | Route | Description | Success | Errors |
|---|---|---|---|---|
| POST | `/api/orders/queue` | Validate an order and place it on `order-processing-queue` | 202 | 400, 503, 500 |
| GET | `/api/orders` | List orders (optional `?status=Received`) | 200 | 400, 500 |
| GET | `/api/orders/{orderDate}/{orderId}` | Get one order | 200 | 404 |
| PUT | `/api/orders/{orderDate}/{orderId}/status` | Move an order to the next status | 200 | 400, 404 |
| Queue | `ProcessOrderQueue` | Saves queued orders to `Orders` and tracks status | - | poison queue |

Example request body:
```json
{
  "OrderId": "ORD-2026-8801",
  "CustomerName": "Jane Smith",
  "SelectedItemSKUs": ["COF-001", "PAS-104"],
  "TotalPrice": 65.00,
  "OrderTimestamp": "2026-10-12T10:15:00Z"
}
```
`OrderId` and `OrderTimestamp` may be left out; the server then generates them.

### Menu (Azure Table Storage), Part 1
| Method | Route | Description |
|---|---|---|
| POST | /api/menu | Create a new menu item |
| GET | /api/menu | Get all menu items |
| GET | /api/menu/category/{category} | Get menu items by category |
| PUT | /api/menu/{category}/{id} | Update a menu item |
| DELETE | /api/menu/{category}/{id} | Delete a menu item |

### Staff Documents (Azure Blob Storage), Part 1
| Method | Route | Description |
|---|---|---|
| POST | /api/documents/upload | Upload a staff document (multipart/form-data) |
| GET | /api/documents | List staff documents with metadata |
| GET | /api/documents/download/{fileName} | Download a staff document |

## Feature Descriptions
### Order Queue Producer (Kandyce, Part 2)
The order producer is the first half of the asynchronous ordering workflow. `SubmitOrder` is an HTTP-triggered function that accepts a new order as JSON at `POST /api/orders/queue` (Microsoft Learn, 2025b). Before anything is queued, it checks that the body is valid JSON, that the customer name is filled in, that at least one item is selected with no blank SKUs and that the total price is above zero. If anything is wrong, a 400 response lists every problem at once so the client can fix them in one go. When the client leaves them out, the server generates the order number and a UTC timestamp. The validated order is then serialised to a JSON string and sent to `order-processing-queue` using Base64 message encoding, which must match the `host.json` setting so that the queue trigger can read the message (Microsoft Learn, 2026h). If the queue cannot be reached, the function catches the error, logs it and returns a 503 within a few seconds instead of crashing. On success it answers 202 Accepted straight away, because the order is processed later in the background (IIE, 2026). `GetOrders` and `GetOrderById` read the `Orders` table and return a clean response object, with a 404 for an unknown order and a 400 for an invalid status filter (Microsoft Learn, 2026c).

### Queue Trigger and Order Tracking (Kristen, Part 2)
`ProcessOrderQueue` is the second half of the workflow and runs automatically whenever a message arrives on `order-processing-queue` (Microsoft Learn, 2026g). It reads the JSON message into an `Order` object and saves an entry in the `Orders` table with the order date as the PartitionKey, the OrderId as the RowKey and the status `Received` (Microsoft Learn, 2026c). It then simulates the barista lifecycle by moving the order through `Preparing` and `Ready` to `Collected`, updating the table row at each step and logging every change. A `PUT` endpoint, `UpdateOrderStatus`, lets staff move an order forward one step at a time and returns a 400 for an invalid or skipped status and a 404 for an unknown order. If a message cannot be processed, the error is logged and the exception is rethrown, so after the default number of attempts the runtime moves the message to `order-processing-queue-poison` instead of blocking the queue (Microsoft Learn, 2026g).

### Docker Compose Orchestration (Thami, Part 2)
One `docker-compose.yml` in the repository root starts the Azurite emulator and the Functions app together with a single `docker compose up` (Docker, 2026f). Both services use version-tagged `v2.0` images from Docker Hub, so a clean machine only needs Docker to run the whole system. The services share a custom bridge network so the Functions container reaches Azurite by the service name `azurite` rather than `localhost` (Docker, 2026e), and a named volume keeps Azurite's data when the containers are removed (Docker, 2026g). The images were built from the multi-stage Dockerfile, tagged `v2.0` and pushed to Docker Hub (Docker, 2026c; Docker, 2026d; Microsoft Learn, 2026b).

### Menu Management (Kandyce, Part 1)
The Menu Management feature uses Microsoft Azure's Table Storage to digitalise CoffeeNChill's chalkboard menu for their coffee shop. With five HTTP triggered Azure Functions that handle full CRUD operations of the menu items (creating, reading, updating, and deleting): create a new menu item (Microsoft Learn, 2025b), read all available menu items (Microsoft Learn, 2026c), read only specific menu items by category (Microsoft Learn, 2022), and updating and deleting menu items (Microsoft Learn, 2026c). This storage system stores each menu item with its category as the Partition Key and a unique SKU as the Row Key. In addition, it includes name, description, price and status fields for each menu item. Validation logic has been implemented in each function to validate the input data when creating a new record (validation for bad request 400 when missing data exists). Also, there have been existence checks included for update and delete operations to prevent unexpected errors from occurring (return a 404 Not Found instead of an unhandled exception).

### Staff Documents (Kristen, Part 1)
The Staff Documents feature allows the canteen staff to upload, view and download important documents using Azure Blob Storage. When a file is uploaded the system checks that it's either a PDF, PNG or JPEG before storing it, so other file types can't be uploaded by mistake (Microsoft Learn, 2026f). The content type is stored with each file so that downloads report the correct file type (Microsoft Learn, 2026i; Microsoft Learn, 2026j). All the uploaded documents are stored in a container called "staff-docs" and users can list every file that's currently stored, along with its name, size and the date it was last modified (Microsoft Learn, 2026e). Files are also downloaded using streaming, which sends the file to the user instead of loading the whole thing into memory first making it more efficient for larger files (Microsoft Learn, 2026d). Each function also includes error handling and logging so any problems such as a missing file or a storage connection issue are caught and logged instead of crashing the app.

### Docker (Thami, Part 1)
My contribution to Part 1 of the POE was the Docker setup. I wrote a multi-stage Dockerfile for the Azure Functions project, the beginning stage used the full .NET 9 SDK image to compile and publish the code, the second stage then copied only the compiled output into the lightweight Azure Functions runtime image, keeping the final image small and clean (Microsoft Learn, 2026a) (Microsoft Learn, 2026b). I also built and tagged both images with version tag v1.0 and published them directly to Docker Hub under the account st10482062: "coffeenchill-functions" for the Azure Functions application and "coffeenchill-azurite" as a re-tagged instance of the official Azurite image (Docker, 2026a), (Docker, 2026b), (Docker, 2026c), (Docker, 2026d). Azurite also runs in its own standalone Docker container with the "--skipApiVersionCheck" flag bound to ports 10000, 10001, and 10002, and both containers communicate with each other over a shared Docker network called "coffeenchill-net" (Microsoft Learn, 2025a), (Microsoft Learn, 2026b).

## Testing
The Postman collections and environment are in `docs/Postman Collection/`: `CoffeeNChill Part 1 - Successful Tests`, `CoffeeNChill Part 1 - Invalid Tests` and the `Local Azurite` environment, which holds `{{baseUrl}}`. The Part 2 requests (queue placement, Orders reads, status updates, poison queue and edge cases) are added to the collections in that folder. Import the collections and the environment, select the "Local Azurite" environment and run the collections. Every request has a saved test script that follows Postman's test-script pattern (Postman, 2025). For Part 2, start the stack with `docker compose up` and close Visual Studio before running them.

Sample staff documents are in `docs/staff-docs_TestDocuments/`: `ChocolateChipCookies_testdata.pdf`, `CoffeeNChill_TestEmployee.pdf` and `Recipe_ButternutChickpeaCurryTEST2.pdf` (PDF files, an allowed upload type) and `How to Make Chocolate Chip Cookies.txt` (a text file, which is not an allowed upload type).

Note: Postman does not export attached files, only a local path to them. Re-select a file from `docs/staff-docs_TestDocuments/` in the Body tab of the Upload request before running it.

**Seeing a message wait in the queue:** add `"AzureWebJobs.ProcessOrderQueue.Disabled": "true"` to `Values` in `local.settings.json`, run the app, place an order and open the queue in Azure Storage Explorer. Remove the line afterwards so the trigger runs again.

**Poison-queue demo:** set `ENABLE_POISON_TEST` to `true` for the `functions` service in `docker-compose.yml`, run `docker compose up` and place an order. The function throws on purpose, and after the default number of attempts the message appears in `order-processing-queue-poison` in Azure Storage Explorer. Set it back to `false` afterwards.

**Evidence:** Part 2 screenshots are in `docs/screenshots_part2/` (one folder per team member) and are also included in the submission document.

## Submission Documents
- Part 1: [CLDV6212_Part_1_Final_Submission.pdf](docs/Final%20Submission%20Document/CLDV6212_Part_1_Final_Submission.pdf)
- Part 2: [CLDV6212_Part_2_Final_Submission.pdf](docs/Final%20Submission%20Document/CLDV6212_Part_2_Final_Submission.pdf)

Each PDF contains the cover page, GitHub, YouTube and Docker Hub links, team member contributions, feature descriptions, evidence screenshots, Postman testing evidence, GitHub contribution and pull request evidence, an AI disclosure and the references.

## Video Demonstrations
- Part 1: https://youtu.be/suzi50aHtrU?si=HFO_WMRrx3LRTdMr
- Part 2: YOUTUBE_LINK_HERE

Set the video quality to 1080p HD in YouTube's settings; it defaults to a lower resolution.

## Important Notes
- **Storage:** the brief mentions Azure File Share. On lecturer guidance, staff documents use Azure Blob Storage (container `staff-docs`) because File Share emulation is not reliably supported in Azurite.
- **.NET version:** the project was scaffolded on .NET 10 and downgraded to .NET 9 for compatibility with the official Azure Functions Docker base images. Install the .NET 9 SDK to build from source.
- **Ports:** Azurite serves Blob on 10000, Queue on 10001 and Table on 10002.

## AI Disclosure
AI tools (Claude) were used for planning, explaining concepts, and reviewing code structure and documentation throughout this project. All code was reviewed, tested, modified where needed and is understood by the team member who committed it. Lecturer examples and Microsoft and Docker documentation are referenced in code comments and below.

## References
Docker, 2026a. Build and push your first image. [Online]
Available at: https://docs.docker.com/get-started/introduction/build-and-push-first-image/
[Accessed 04 September 2026].

Docker, 2026b. Docker Hub quickstart. [Online]
Available at: https://docs.docker.com/docker-hub/quickstart/
[Accessed 04 September 2026].

Docker, 2026c. Push images to a repository. [Online]
Available at: https://docs.docker.com/docker-hub/repos/manage/hub-images/push/
[Accessed 04 September 2026].

Docker, 2026d. Build, tag, and publish an image. [Online]
Available at: https://docs.docker.com/get-started/docker-concepts/building-images/build-tag-and-publish-an-image/
[Accessed 04 September 2026].

Docker, 2026e. Define and manage networks in Docker Compose. [Online]
Available at: https://docs.docker.com/reference/compose-file/networks/
[Accessed 29 September 2026].

Docker, 2026f. Compose file reference. [Online]
Available at: https://docs.docker.com/reference/compose-file/
[Accessed 29 September 2026].

Docker, 2026g. Define and manage volumes in Docker Compose. [Online]
Available at: https://docs.docker.com/reference/compose-file/volumes/
[Accessed 29 September 2026].

IIE, 2026. CLDV6212 lecturer course examples and notes (Parts 1 and 2): Azure Functions, Table and Blob Storage, queue triggers and Docker Compose. [Lecture notes]. Cloud Development B. The Independent Institute of Education.

Microsoft Learn, 2022. What is Azure Table storage?. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
[Accessed 04 September 2026].

Microsoft Learn, 2025a. Install and run Azurite emulator. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite
[Accessed 04 September 2026].

Microsoft Learn, 2025b. Azure Functions HTTP trigger. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger
[Accessed 04 September 2026].

Microsoft Learn, 2026a. Create your first containerized Azure Functions. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-deploy-container
[Accessed 04 September 2026].

Microsoft Learn, 2026b. Work with containers and Azure Functions. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-custom-container
[Accessed 04 September 2026].

Microsoft Learn, 2026c. Azure Tables client library for .NET - version 12.12.0. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
[Accessed 04 September 2026].

Microsoft Learn, 2026d. Download a blob with .NET. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-download
[Accessed 04 September 2026].

Microsoft Learn, 2026e. Introduction to Azure Blob Storage. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction
[Accessed 04 September 2026].

Microsoft Learn, 2026f. MultipartReader Class. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.webutilities.multipartreader?view=aspnetcore-10.0
[Accessed 04 September 2026].

Microsoft Learn, 2026g. Azure Queue storage trigger and bindings for Azure Functions overview. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue
[Accessed 29 September 2026].

Microsoft Learn, 2026h. Azure Storage Queues client library for .NET. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.queues-readme
[Accessed 29 September 2026].

Microsoft Learn, 2026i. BlobHttpHeaders Class. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobhttpheaders?view=azure-dotnet
[Accessed 04 September 2026].

Microsoft Learn, 2026j. BlobUploadOptions.HttpHeaders Property. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobuploadoptions.httpheaders?view=azure-dotnet
[Accessed 04 September 2026].

Mrzyglod, K. and Camden, R., 2020. Azure for Developers: Implement rich Azure PaaS ecosystems using containers, serverless services, and storage solutions. 2nd ed. Birmingham: Packt Publishing.

Postman, 2025. Write scripts to test API response data in Postman. [Online]
Available at: https://learning.postman.com/docs/tests-and-scripts/write-scripts/test-scripts/
[Accessed 01 September 2026].