using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Application.Services;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BoneAI.Tests.Services;

public class ImageServiceTests
{
    private readonly Mock<IXRayImageRepository> _mockRepo;
    private readonly Mock<IImageStorageService> _mockStorage;
    private readonly Mock<IImageValidator> _mockValidator;
    private readonly ImageService _service;

    public ImageServiceTests()
    {
        _mockRepo = new Mock<IXRayImageRepository>();
        _mockStorage = new Mock<IImageStorageService>();
        _mockValidator = new Mock<IImageValidator>();

        _service = new ImageService(
            _mockRepo.Object,
            _mockStorage.Object,
            _mockValidator.Object,
            NullLogger<ImageService>.Instance);
    }

    [Fact]
    public async Task UploadAsync_ValidImage_ReturnsCreatedResponse()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var request = new UploadImageRequest
        {
            FileStream = stream,
            FileName = "wrist.jpg",
            ContentType = "image/jpeg",
            Length = 3
        };

        _mockValidator
            .Setup(v => v.ValidateAsync(stream, "wrist.jpg", "image/jpeg", 3L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageValidationResult(true, "JPEG", "image/jpeg", 500, 500, 3));

        _mockStorage
            .Setup(s => s.SaveAsync(stream, "wrist.jpg", "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync("2026/11/guid123.jpg");

        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<XRayImage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.UploadAsync(request);

        Assert.NotNull(result);
        Assert.Equal("wrist.jpg", result.FileName);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal($"/api/images/{result.Id}/file", result.FileUrl);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<XRayImage>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_StorageFails_DoesNotPersistToDbAndThrows()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var request = new UploadImageRequest
        {
            FileStream = stream,
            FileName = "wrist.jpg",
            ContentType = "image/jpeg",
            Length = 3
        };

        _mockValidator
            .Setup(v => v.ValidateAsync(stream, "wrist.jpg", "image/jpeg", 3L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageValidationResult(true, "JPEG", "image/jpeg", 500, 500, 3));

        _mockStorage
            .Setup(s => s.SaveAsync(stream, "wrist.jpg", "image/jpeg", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full"));

        await Assert.ThrowsAsync<IOException>(() => _service.UploadAsync(request));

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<XRayImage>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_DbFailsAfterStorage_RollsBackStorageFile()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var request = new UploadImageRequest
        {
            FileStream = stream,
            FileName = "wrist.jpg",
            ContentType = "image/jpeg",
            Length = 3
        };

        _mockValidator
            .Setup(v => v.ValidateAsync(stream, "wrist.jpg", "image/jpeg", 3L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageValidationResult(true, "JPEG", "image/jpeg", 500, 500, 3));

        _mockStorage
            .Setup(s => s.SaveAsync(stream, "wrist.jpg", "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync("2026/11/orphan.jpg");

        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failure"));

        _mockStorage
            .Setup(s => s.DeleteAsync("2026/11/orphan.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<Exception>(() => _service.UploadAsync(request));

        // Verify compensation rollback was executed!
        _mockStorage.Verify(s => s.DeleteAsync("2026/11/orphan.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedImages()
    {
        var images = new List<XRayImage>
        {
            new() { Id = Guid.NewGuid(), FileName = "img1.jpg", FilePath = "k1", ContentType = "image/jpeg", FileSize = 100, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), FileName = "img2.png", FilePath = "k2", ContentType = "image/png", FileSize = 200, CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
        };

        _mockRepo
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((images, 2));

        var response = await _service.GetPagedAsync(1, 10);

        Assert.Equal(2, response.TotalCount);
        Assert.Equal(1, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal("img1.jpg", response.Items[0].FileName);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingImage_ReturnsMetadata()
    {
        var id = Guid.NewGuid();
        var image = new XRayImage
        {
            Id = id,
            FileName = "xray.jpg",
            FilePath = "2026/11/xray.jpg",
            ContentType = "image/jpeg",
            FileSize = 5000,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(image);

        var result = await _service.GetByIdAsync(id);

        Assert.Equal(id, result.Id);
        Assert.Equal("xray.jpg", result.FileName);
        Assert.Equal($"/api/images/{id}/file", result.FileUrl);
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentImage_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((XRayImage?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));
    }

    [Fact]
    public async Task GetFileAsync_ExistingFile_ReturnsStream()
    {
        var id = Guid.NewGuid();
        var image = new XRayImage
        {
            Id = id,
            FileName = "xray.jpg",
            FilePath = "2026/11/xray.jpg",
            ContentType = "image/jpeg"
        };

        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockStorage.Setup(s => s.GetAsync("2026/11/xray.jpg", It.IsAny<CancellationToken>())).ReturnsAsync(stream);

        var result = await _service.GetFileAsync(id);

        Assert.NotNull(result);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal("xray.jpg", result.FileName);
    }

    [Fact]
    public async Task GetFileAsync_MissingPhysicalFile_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var image = new XRayImage
        {
            Id = id,
            FilePath = "missing.jpg"
        };

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockStorage.Setup(s => s.GetAsync("missing.jpg", It.IsAny<CancellationToken>())).ReturnsAsync((Stream?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetFileAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_NoAnalysisReferences_DeletesFileAndDbRecord()
    {
        var id = Guid.NewGuid();
        var image = new XRayImage
        {
            Id = id,
            FilePath = "2026/11/del.jpg"
        };

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockRepo.Setup(r => r.HasAnalysisReferencesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockStorage.Setup(s => s.DeleteAsync("2026/11/del.jpg", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockRepo.Setup(r => r.DeleteAsync(image, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _service.DeleteAsync(id);

        _mockStorage.Verify(s => s.DeleteAsync("2026/11/del.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.DeleteAsync(image, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReferencedByAnalysis_ThrowsConflictExceptionAndPreservesData()
    {
        var id = Guid.NewGuid();
        var image = new XRayImage
        {
            Id = id,
            FilePath = "2026/11/referenced.jpg"
        };

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockRepo.Setup(r => r.HasAnalysisReferencesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(id));

        _mockStorage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<XRayImage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonexistentImage_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((XRayImage?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_StorageDeletionFails_AbortsAndPreservesDbMetadata()
    {
        var id = Guid.NewGuid();
        var image = new XRayImage
        {
            Id = id,
            FilePath = "2026/11/locked.jpg"
        };

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(image);
        _mockRepo.Setup(r => r.HasAnalysisReferencesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockStorage.Setup(s => s.DeleteAsync("2026/11/locked.jpg", It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("File locked"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(id));

        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<XRayImage>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
