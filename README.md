# CoffeeNChill Canteen Management System - Part 1

## Group number and members
Group on ARC: 7 

Members:

Kristen Keve - ST10472683 

Kandyce Smit - ST10467645

Thamsanqa Ncube - ST10482062 

## Project Overview
This project modernises the CoffeeNChill campus canteen's paper-based menu and
filing cabinet system into a cloud-enabled backend. Part 1 builds the foundation:
Azure Table Storage for the digital menu, Azure Blob Storage for staff documents,
and standalone Docker containers for local Azurite emulation and the Azure
Functions app itself.

## Repository Note
The original project repository was created through the university's
GitHub Classroom and was configured as a private repository. As the
project submission requires a publicly accessible GitHub repository,
the completed project was mirrored to a separate public repository
for submission and assessment purposes. The public repository
contains the same project source code, documentation, testing
materials, and submission evidence as the original Classroom
repository.

Private repository accepted from Classroom 50: https://github.com/EMGPSD/cldv6212-g1-2026-poe-part1-st10467645.git

Public repository created by group: https://github.com/ST10467645/CLDV6212-g1-CoffeeNChill-ArcGroup7.git

## Important Note on Storage
The assignment brief references "Azure File Share" for staff documents. Per
lecturer guidance, Azure File Share emulation was replaced with Azure Blob
Storage (container: staff-docs) to provide equivalent document upload/list/
download functionality, as Azure File Share emulation is not reliably
supported in Azurite for this project.

## Important Note on .NET Version
This project was originally scaffolded on .NET 10, but was downgraded to
.NET 9 during development for compatibility with the official Azure Functions
Docker base images used in containerisation. Ensure the .NET 9 SDK is
installed before building or running this project.

## Team Members and Contributions
| Name | Student Number | Role | What they built |
|---|---|---|---|
| Kandyce Smit | ST10467645 | Menu CRUD & Documentation Lead | MenuItem model, all 5 Menu HTTP functions (Create, GetAll, GetByCategory, Update, Delete), README |
| Kristen Keve | ST10472683 | Staff Documents (Blob Storage) | UploadStaffDocument, ListStaffDocuments, DownloadStaffDocument functions |
| Thamsanqa Ncube | ST10482062 | Docker & Docker Hub | Dockerfile, Docker Hub image publishing, Azurite container setup |

## Prerequisites (Tools to Install)
- Visual Studio 2022 (with Azure development workload)
- .NET 9 SDK
- Docker Desktop
- Postman
- Git

## Local Setup Instructions

1. Clone the repository:

```text
git clone https://github.com/ST10467645/CLDV6212-g1-CoffeeNChill-ArcGroup7.git

```

2. Ensure that a local.settings.json file is present in the project root
with the following content:

```text
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}

```

3. Start the Azurite storage emulator in Docker:

```text
docker run -d --name coffeenchill-azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite azurite --skipApiVersionCheck --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0

```

4. Open the solution in Visual Studio and press F5 to run the Functions project locally.

## Project Structure

```text
CoffeeNChillFunctions/
├── docs/
│   ├── docker-screenshots/
│   ├── Final Submission Document/
│   ├── Postman Collection/
│   └── staff-docs_TestDocuments/
├── Functions/
├── Models/
├── Properties/
├── Dockerfile
├── local.settings.json
├── CoffeeNChillFunctions.csproj
└── README.md

```
Note: bin/ and obj/ folders are auto-generated build output and are
excluded from version control via .gitignore.

## Docker Standalone Container Commands
See docs/docker-commands.md for the full reference of Docker commands used
in this project (build, push, run commands for both the Functions image and
the Azurite image).

## Docker Hub Images
- Functions image: https://hub.docker.com/r/st10482062/coffeenchill-functions
- Azurite image: https://hub.docker.com/r/st10482062/coffeenchill-azurite

## API Endpoints

### Menu (Azure Table Storage)
| Method | Route | Description |
|---|---|---|
| POST | /api/menu | Create a new menu item |
| GET | /api/menu | Get all menu items |
| GET | /api/menu/category/{category} | Get menu items filtered by category |
| PUT | /api/menu/{category}/{id} | Update a menu item's price or availability |
| DELETE | /api/menu/{category}/{id} | Delete a menu item |

### Staff Documents (Azure Blob Storage)
| Method | Route | Description |
|---|---|---|
| POST | /api/documents/upload | Upload a staff document (multipart/form-data) |
| GET | /api/documents | List all staff documents with metadata |
| GET | /api/documents/download/{fileName} | Download a specific staff document |

## Feature Descriptions

### Menu Management (Kandyce)
The Menu Management feature uses Microsoft Azure's Table Storage to digitalise
CoffeeNChill's chalkboard menu for their coffee shop. With five HTTP triggered
Azure Functions that handle full CRUD operations of the menu items (creating,
reading, updating, and deleting): create a new menu item (Microsoft Learn,
2025b), read all available menu items (Microsoft Learn, 2026c), read only
specific menu items by category (Microsoft Learn, 2022), and updating and
deleting menu items (Microsoft Learn, 2026c). This storage system stores each
menu item with its category as the Partition Key and a unique SKU as the Row
Key. In addition, it includes name, description, price and status fields for
each menu item. Validation logic has been implemented in each function to
validate the input data when creating a new record (validation for bad
request 400 when missing data exists). Also, there have been existence checks
included for update and delete operations to prevent unexpected errors from
occurring (return a 404 Not Found instead of an unhandled exception).

### Staff Documents (Kristen)
The Staff Documents feature allows the canteen staff to upload, view and
download important documents using Azure Blob Storage. When a file is
uploaded the system checks that it's either a PDF, PNG or JPEG before storing
it, so other file types can't be uploaded by mistake (Microsoft Learn, 2026f).
All the uploaded documents are stored in a container called "staff-docs" and
users can list every file that's currently stored, along with its name, size
and the date it was last modified (Microsoft Learn, 2026e). Files are also
downloaded using streaming, which sends the file to the user instead of
loading the whole thing into memory first making it more efficient for larger
files (Microsoft Learn, 2026d). Each function also includes error handling
and logging so any problems such as a missing file or a storage connection
issue are caught and logged instead of crashing the app.

The test documents used for the Staff Documents upload functionality can
be found in the `docs/staff-docs_TestDocuments/` folder. These files are
provided as sample documents for testing the upload endpoint and
demonstrating the supported document types.


### Docker (Thami)
My contribution to Part 1 of the POE was the Docker setup. I wrote a
multi-stage Dockerfile for the Azure Functions project, the beginning stage
used the full .NET 9 SDK image to compile and publish the code, the second
stage then copied only the compiled output into the lightweight Azure
Functions runtime image, keeping the final image small and clean (Microsoft
Learn, 2026a) (Microsoft Learn, 2026b). I also built and tagged both images
with version tag v1.0 and published them directly to Docker Hub under the
account st10482062: "coffeenchill-functions" for the Azure Functions
application and "coffeenchill-azurite" as a re-tagged instance of the
official Azurite image (Docker, 2026a), (Docker, 2026b), (Docker, 2026c),
(Docker, 2026d). Azurite also runs in its own standalone Docker container
with the "--skipApiVersionCheck" flag bound to ports 10000, 10001, and 10002,
and both containers communicate with each other over a shared Docker network
called "coffeenchill-net" (Microsoft Learn, 2025a), (Microsoft Learn, 2026b).

## Testing
A full Postman collection covering every endpoint above, with sample request
bodies and passing tests, is available in `docs/Postman Collection/`. Import
the collection and environment files into Postman, select the "Local
Azurite" environment, and run the collection.

For the Staff Documents functionality, the test documents used for uploading
files are available in `docs/staff-docs_TestDocuments/`. The folder contains
the sample files used to test the document upload functionality.

Note: if testing the Upload Staff Document request via Postman's "Run
collection" feature on a machine other than the one the file was originally
attached on, you may need to re-select the file in the Body tab first, as
Postman does not export the actual file with the collection, only a local
file path reference.

## Final Submission Document

The full Part 1 POE document is available as a PDF in:

docs/Final Submission Document/CLDV6212_Part_1_Final_Submission.pdf

The PDF contains the cover page, GitHub and YouTube links, team member contributions, feature descriptions, evidence screenshots, Postman testing evidence, GitHub contribution and pull request evidence, and references. This PDF serves as the formal Part 1 submission document referenced in the assignment brief.


## Video Demonstration
https://youtu.be/suzi50aHtrU?si=HFO_WMRrx3LRTdMr

Note: please set the video quality to 1080p HD in YouTube's settings, as it
will default to 360p on load and may appear blurry otherwise.

## Referencing

### Section A - Feature Implementation Descriptions
The descriptions above for Menu Management, Staff Documents, and Docker were
written using official Microsoft Learn and Docker documentation as
references for the Azure Functions, Azure Table Storage, Azure Blob Storage,
and Docker concepts and APIs used throughout this project.

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

Microsoft Learn, 2022. What is Azure Table storage?. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
[Accessed 04 September 2026].

Microsoft Learn, 2025a. Install and run Azurite emulator. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite
[Accessed 04 September 2026].

Microsoft Learn, 2025b. Azure Functions HTTP trigger. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cfunctionsv2&pivots=programming-language-csharp
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
