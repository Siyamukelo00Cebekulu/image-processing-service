# Image Processing Service

A full-stack image processing application with a .NET 8 backend and a React frontend. The service supports authenticated image upload, transformation, retrieval, and listing, with SQLite for metadata, local disk storage for image files, and ImageSharp for server-side image processing.

## Features

- User registration and login with JWT issuance
- Auth-protected image upload
- Transformations: resize, crop, rotate, watermark, flip, mirror, compress, format conversion, grayscale, sepia
- Cached transformed variants based on transformation signature
- Paginated image listing with metadata
- Image retrieval with optional format conversion on download
- Centralized error handling and request validation

## Technology Stack

### Backend

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- JWT Bearer Authentication
- ASP.NET Core Rate Limiting
- SixLabors.ImageSharp
- SixLabors.ImageSharp.Drawing

### Frontend

- React 18
- Vite
- JavaScript (ES modules)
- CSS

### Storage and Persistence

- SQLite database for users and image metadata
- Local file system storage for uploaded and transformed images

### Development Tooling

- .NET CLI
- npm
- Vite dev server

## Project Structure

- `backend/ImageProcessingService.sln` - .NET solution
- `backend/ImageProcessingService.Api` - ASP.NET Core API project
- `frontend` - React client application
- `backend/ImageProcessingService.Api/Controllers` - API endpoints
- `backend/ImageProcessingService.Api/Data` - EF Core context
- `backend/ImageProcessingService.Api/Models` - persistence models
- `backend/ImageProcessingService.Api/Services` - auth, storage, and image processing services
- `backend/ImageProcessingService.Api/Contracts` - request and response DTOs
- `frontend/src` - React application source

## Run Locally

1. Restore packages:

```powershell
dotnet restore backend/ImageProcessingService.Api/ImageProcessingService.Api.csproj --configfile backend/ImageProcessingService.Api/NuGet.Config
```

2. Start the API:

```powershell
dotnet run --project backend/ImageProcessingService.Api
```

3. Install frontend dependencies:

```powershell
cd frontend
npm install
```

4. Start the React client:

```powershell
npm run dev
```

The API creates `image-processing.db` for metadata and `backend/ImageProcessingService.Api/Storage` for persisted files on first run.
The frontend runs on `http://localhost:5173` and proxies API requests to the .NET service on `http://localhost:5228`.

## How It Works

- Users register and log in through JWT-based authentication endpoints.
- Authenticated users can upload images and store them on the server.
- Transformations are processed on the backend with ImageSharp.
- Metadata is stored in SQLite, while image binaries are saved to disk.
- Previously generated transformations are reused through a transformation signature cache.

## Configuration

Update `backend/ImageProcessingService.Api/appsettings.json` before production use:

- `Jwt:SecretKey` - set a long random secret
- `ConnectionStrings:DefaultConnection` - adjust database location if needed
- `Storage:RootPath` - swap local storage path

For the frontend, you can optionally provide `VITE_API_BASE_URL` if you want the React app to call a non-default API base URL instead of using the local Vite proxy.

## API Endpoints

### Auth

`POST /register`

```json
{
  "username": "user1",
  "password": "password123"
}
```

`POST /login`

```json
{
  "username": "user1",
  "password": "password123"
}
```

### Images

`POST /images`

- Multipart form-data with field name `file`

`POST /images/{id}/transform`

```json
{
  "transformations": {
    "resize": {
      "width": 800,
      "height": 600
    },
    "crop": {
      "width": 500,
      "height": 500,
      "x": 10,
      "y": 10
    },
    "rotate": 90,
    "watermark": {
      "text": "sample"
    },
    "flip": false,
    "mirror": true,
    "quality": 80,
    "format": "png",
    "filters": {
      "grayscale": false,
      "sepia": true
    }
  }
}
```

`GET /images/{id}`

- Returns the stored image file
- Optional query: `?format=png`

`GET /images?page=1&limit=10`

- Returns paginated image metadata for the authenticated user

## Notes

- Transformation requests are rate limited to 20 per minute per authenticated user.
- Transformed image variants are cached in storage and metadata by hashing the transformation payload.
- The storage layer is abstracted behind `IFileStorageService`, so local disk can be replaced with S3, R2, or GCS later without changing controller logic.
- The frontend is intentionally minimal and is designed as a lightweight control panel for the API.

## Project Reference

https://roadmap.sh/projects/image-processing-service
