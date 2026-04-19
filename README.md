# Image Processing Service

A .NET 8 Web API for authenticated image upload, transformation, retrieval, and listing. The service uses JWT authentication, SQLite for metadata, local disk storage for image binaries, ImageSharp for processing, and built-in ASP.NET Core rate limiting for transformation protection.

## Features

- User registration and login with JWT issuance
- Auth-protected image upload
- Transformations: resize, crop, rotate, watermark, flip, mirror, compress, format conversion, grayscale, sepia
- Cached transformed variants based on transformation signature
- Paginated image listing with metadata
- Image retrieval with optional format conversion on download
- Centralized error handling and request validation

## Stack

- .NET 8 / ASP.NET Core Web API
- SQLite with EF Core
- JWT Bearer authentication
- SixLabors.ImageSharp

## Project Structure

- `backend/ImageProcessingService.sln` - .NET solution
- `backend/ImageProcessingService.Api` - ASP.NET Core API project
- `frontend` - React client application
- `backend/ImageProcessingService.Api/Controllers` - API endpoints
- `backend/ImageProcessingService.Api/Data` - EF Core context
- `backend/ImageProcessingService.Api/Models` - persistence models
- `backend/ImageProcessingService.Api/Services` - auth, storage, and image processing services
- `backend/ImageProcessingService.Api/Contracts` - request and response DTOs

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

## Configuration

Update `backend/ImageProcessingService.Api/appsettings.json` before production use:

- `Jwt:SecretKey` - set a long random secret
- `ConnectionStrings:DefaultConnection` - adjust database location if needed
- `Storage:RootPath` - swap local storage path

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
