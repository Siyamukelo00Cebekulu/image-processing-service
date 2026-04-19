using ImageProcessingService.Api.Contracts.Images;
using ImageProcessingService.Api.Data;
using ImageProcessingService.Api.Extensions;
using ImageProcessingService.Api.Models;
using ImageProcessingService.Api.Services;
using ImageProcessingService.Api.Services.ImageProcessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ImageProcessingService.Api.Controllers;

[ApiController]
[Route("images")]
[Authorize]
public sealed class ImagesController(
    ApplicationDbContext dbContext,
    IImageProcessor imageProcessor,
    IFileStorageService fileStorageService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [ProducesResponseType(typeof(ImageResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ImageResponse>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new { message = "An image file is required." });
        }

        var validated = await imageProcessor.ValidateUploadAsync(file, cancellationToken);
        var userId = User.GetUserId();
        var storageKey = BuildStorageKey(userId, validated.Extension);

        await fileStorageService.SaveAsync(storageKey, validated.Content, cancellationToken);

        var image = new ImageAsset
        {
            UserId = userId,
            FileName = Path.GetFileName(file.FileName),
            StorageKey = storageKey,
            ContentType = validated.ContentType,
            Format = validated.Format,
            SizeBytes = validated.SizeBytes,
            Width = validated.Width,
            Height = validated.Height,
            IsTransformed = false
        };

        dbContext.Images.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = image.Id }, image.ToResponse());
    }

    [HttpPost("{id:guid}/transform")]
    [EnableRateLimiting("transformations")]
    [ProducesResponseType(typeof(ImageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImageResponse>> Transform(Guid id, [FromBody] TransformImageRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var sourceImage = await dbContext.Images
            .SingleOrDefaultAsync(image => image.Id == id && image.UserId == userId, cancellationToken);

        if (sourceImage is null)
        {
            return NotFound(new { message = "Image not found." });
        }

        await using var sourceStream = await fileStorageService.OpenReadAsync(sourceImage.StorageKey, cancellationToken);
        var transformed = await imageProcessor.TransformAsync(sourceStream, request.Transformations, cancellationToken);

        var existing = await dbContext.Images.SingleOrDefaultAsync(image =>
            image.UserId == userId &&
            image.OriginalImageId == sourceImage.Id &&
            image.TransformationSignature == transformed.TransformationSignature,
            cancellationToken);

        if (existing is not null)
        {
            return Ok(existing.ToResponse());
        }

        var transformedFileName = $"{Path.GetFileNameWithoutExtension(sourceImage.FileName)}-{transformed.TransformationSignature[..8]}{transformed.Extension}";
        var storageKey = BuildStorageKey(userId, transformed.Extension);
        await fileStorageService.SaveAsync(storageKey, transformed.Content, cancellationToken);

        var image = new ImageAsset
        {
            UserId = userId,
            OriginalImageId = sourceImage.Id,
            FileName = transformedFileName,
            StorageKey = storageKey,
            ContentType = transformed.ContentType,
            Format = transformed.Format,
            SizeBytes = transformed.SizeBytes,
            Width = transformed.Width,
            Height = transformed.Height,
            IsTransformed = true,
            TransformationSignature = transformed.TransformationSignature,
            TransformationSummary = transformed.TransformationSummary
        };

        dbContext.Images.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(image.ToResponse());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var image = await dbContext.Images.SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId, cancellationToken);
        if (image is null)
        {
            return NotFound(new { message = "Image not found." });
        }

        await using var source = await fileStorageService.OpenReadAsync(image.StorageKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(format) || string.Equals(format, image.Format, StringComparison.OrdinalIgnoreCase))
        {
            var output = new MemoryStream();
            await source.CopyToAsync(output, cancellationToken);
            output.Position = 0;
            return File(output, image.ContentType, image.FileName);
        }

        var transformed = await imageProcessor.TransformAsync(source, new TransformationRequest { Format = format }, cancellationToken);
        return File(transformed.Content, transformed.ContentType, $"{Path.GetFileNameWithoutExtension(image.FileName)}{transformed.Extension}");
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ImageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ImageResponse>>> List([FromQuery] int page = 1, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 100);
        var userId = User.GetUserId();

        var query = dbContext.Images
            .AsNoTracking()
            .Where(image => image.UserId == userId)
            .OrderByDescending(image => image.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<ImageResponse>
        {
            Items = items.Select(image => image.ToResponse()).ToList(),
            Page = page,
            Limit = limit,
            TotalCount = totalCount
        });
    }

    private static string BuildStorageKey(Guid userId, string extension) =>
        Path.Combine(userId.ToString("N"), $"{Guid.NewGuid():N}{extension}");
}
