using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ImageProcessingService.Api.Contracts.Images;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace ImageProcessingService.Api.Services.ImageProcessing;

public sealed class ImageSharpProcessor : IImageProcessor
{
    private const long MaxFileSizeBytes = 15 * 1024 * 1024;
    private static readonly HashSet<string> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpeg",
        "jpg",
        "png",
        "webp"
    };

    public async Task<ImageValidationResult> ValidateUploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("The uploaded file is empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("The uploaded file exceeds the 15 MB size limit.");
        }

        await using var uploadStream = file.OpenReadStream();
        var buffer = new MemoryStream();
        await uploadStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var info = await Image.IdentifyAsync(buffer, cancellationToken);
        if (info is null || info.Metadata.DecodedImageFormat is null)
        {
            throw new InvalidOperationException("The uploaded file is not a supported image.");
        }

        var format = NormalizeFormat(info.Metadata.DecodedImageFormat);
        if (!SupportedFormats.Contains(format))
        {
            throw new InvalidOperationException("Only JPEG, PNG, and WebP uploads are supported.");
        }

        buffer.Position = 0;
        return new ImageValidationResult
        {
            Content = buffer,
            ContentType = GetContentType(format),
            Extension = GetExtension(format),
            Format = format,
            Width = info.Width,
            Height = info.Height,
            SizeBytes = buffer.Length
        };
    }

    public async Task<ProcessedImageResult> TransformAsync(Stream source, TransformationRequest request, CancellationToken cancellationToken)
    {
        source.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(source, cancellationToken);
        var sourceFormat = image.Metadata.DecodedImageFormat is null
            ? "jpeg"
            : NormalizeFormat(image.Metadata.DecodedImageFormat);

        ValidateTransformationRequest(image.Width, image.Height, request);

        image.Mutate(context =>
        {
            if (request.Resize is not null && (request.Resize.Width.HasValue || request.Resize.Height.HasValue))
            {
                context.Resize(new ResizeOptions
                {
                    Size = new Size(request.Resize.Width ?? 0, request.Resize.Height ?? 0),
                    Mode = ResizeMode.Max
                });
            }

            if (request.Crop is not null)
            {
                context.Crop(new Rectangle(request.Crop.X, request.Crop.Y, request.Crop.Width, request.Crop.Height));
            }

            if (request.Rotate.HasValue && Math.Abs(request.Rotate.Value) > 0.01f)
            {
                context.Rotate(request.Rotate.Value);
            }

            if (request.Flip)
            {
                context.Flip(FlipMode.Vertical);
            }

            if (request.Mirror)
            {
                context.Flip(FlipMode.Horizontal);
            }

            if (request.Filters?.Grayscale == true)
            {
                context.Grayscale();
            }

            if (request.Filters?.Sepia == true)
            {
                context.Sepia();
            }
        });

        if (!string.IsNullOrWhiteSpace(request.Watermark?.Text))
        {
            var font = SystemFonts.CreateFont("Arial", 24);
            var padding = Math.Max(request.Watermark.Padding, 0);
            var location = new PointF(padding, Math.Max(image.Height - 40 - padding, padding));
            image.Mutate(context =>
            {
                context.DrawText(request.Watermark.Text, font, Color.White.WithAlpha(0.72f), location);
            });
        }

        var targetFormat = NormalizeTargetFormat(request.Format);
        if (string.IsNullOrWhiteSpace(targetFormat))
        {
            targetFormat = sourceFormat;
        }

        var quality = Math.Clamp(request.Quality ?? 85, 10, 100);
        var output = new MemoryStream();
        await image.SaveAsync(output, GetEncoder(targetFormat, quality), cancellationToken);
        output.Position = 0;

        var signature = ComputeSignature(request);
        return new ProcessedImageResult
        {
            Content = output,
            ContentType = GetContentType(targetFormat),
            Extension = GetExtension(targetFormat),
            Format = targetFormat,
            Width = image.Width,
            Height = image.Height,
            SizeBytes = output.Length,
            TransformationSignature = signature,
            TransformationSummary = BuildSummary(request)
        };
    }

    private static string ComputeSignature(TransformationRequest request)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildSummary(TransformationRequest request)
    {
        var operations = new List<string>();

        if (request.Resize is not null && (request.Resize.Width.HasValue || request.Resize.Height.HasValue))
        {
            operations.Add($"resize({request.Resize.Width?.ToString() ?? "auto"}x{request.Resize.Height?.ToString() ?? "auto"})");
        }

        if (request.Crop is not null)
        {
            operations.Add($"crop({request.Crop.Width}x{request.Crop.Height}@{request.Crop.X},{request.Crop.Y})");
        }

        if (request.Rotate.HasValue)
        {
            operations.Add($"rotate({request.Rotate.Value})");
        }

        if (request.Flip)
        {
            operations.Add("flip(vertical)");
        }

        if (request.Mirror)
        {
            operations.Add("mirror(horizontal)");
        }

        if (request.Filters?.Grayscale == true)
        {
            operations.Add("grayscale");
        }

        if (request.Filters?.Sepia == true)
        {
            operations.Add("sepia");
        }

        if (!string.IsNullOrWhiteSpace(request.Watermark?.Text))
        {
            operations.Add("watermark");
        }

        if (request.Quality.HasValue)
        {
            operations.Add($"compress({request.Quality.Value})");
        }

        if (!string.IsNullOrWhiteSpace(request.Format))
        {
            operations.Add($"format({request.Format})");
        }

        return operations.Count == 0 ? "no-op" : string.Join(", ", operations);
    }

    private static string NormalizeTargetFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return string.Empty;
        }

        var normalized = format.Trim().TrimStart('.').ToLowerInvariant();
        normalized = normalized == "jpg" ? "jpeg" : normalized;

        if (!SupportedFormats.Contains(normalized))
        {
            throw new InvalidOperationException("Target format must be jpeg, png, or webp.");
        }

        return normalized;
    }

    private static string NormalizeFormat(IImageFormat format)
    {
        var name = format.Name.ToLowerInvariant();
        return name == "jpg" ? "jpeg" : name;
    }

    private static IImageEncoder GetEncoder(string format, int quality) =>
        format switch
        {
            "png" => new PngEncoder(),
            "webp" => new WebpEncoder { Quality = quality },
            _ => new JpegEncoder { Quality = quality }
        };

    private static string GetContentType(string format) =>
        format switch
        {
            "png" => "image/png",
            "webp" => "image/webp",
            _ => "image/jpeg"
        };

    private static string GetExtension(string format) =>
        format switch
        {
            "png" => ".png",
            "webp" => ".webp",
            _ => ".jpg"
        };

    private static void ValidateTransformationRequest(int imageWidth, int imageHeight, TransformationRequest request)
    {
        if (request.Resize is not null)
        {
            if (request.Resize.Width is <= 0 || request.Resize.Height is <= 0)
            {
                throw new InvalidOperationException("Resize width and height must be greater than zero when provided.");
            }
        }

        if (request.Crop is not null)
        {
            if (request.Crop.Width <= 0 || request.Crop.Height <= 0)
            {
                throw new InvalidOperationException("Crop width and height must be greater than zero.");
            }

            if (request.Crop.X < 0 || request.Crop.Y < 0)
            {
                throw new InvalidOperationException("Crop coordinates must be zero or greater.");
            }

            if (request.Crop.X + request.Crop.Width > imageWidth || request.Crop.Y + request.Crop.Height > imageHeight)
            {
                throw new InvalidOperationException("Crop dimensions exceed the image bounds.");
            }
        }

        if (request.Quality is < 1 or > 100)
        {
            throw new InvalidOperationException("Compression quality must be between 1 and 100.");
        }
    }
}
