using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Common.Models;
using LastMile.TMS.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class S3CompatibleFileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageOptions _options;
    private readonly ILogger<S3CompatibleFileStorageService> _logger;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private int _bucketReady;

    public S3CompatibleFileStorageService(
        IOptions<StorageOptions> options,
        ILogger<S3CompatibleFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ForcePathStyle = _options.ForcePathStyle,
            AuthenticationRegion = _options.Region,
        };

        if (!string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            config.ServiceURL = _options.Endpoint;
        }
        else
        {
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region);
        }

        _s3Client = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKey, _options.SecretKey),
            config);
    }

    public async Task UploadAsync(
        string key,
        Stream content,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureBucketAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<FileStorageReadResult> OpenReadAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureBucketAsync(cancellationToken);

        try
        {
            using var response = await _s3Client.GetObjectAsync(_options.BucketName, key, cancellationToken);
            var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            return new FileStorageReadResult(buffer, response.Headers.ContentType, response.Headers.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Stored object '{key}' was not found.", key, ex);
        }
    }

    public async Task DeleteIfExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureBucketAsync(cancellationToken);

        try
        {
            await _s3Client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Stored object {Key} was already absent from bucket {Bucket}.", key, _options.BucketName);
        }
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        var keys = new List<string>();
        string? continuationToken = null;

        do
        {
            var response = await _s3Client.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = _options.BucketName,
                    Prefix = prefix ?? string.Empty,
                    ContinuationToken = continuationToken,
                },
                cancellationToken);

            keys.AddRange(response.S3Objects.Select(x => x.Key));
            continuationToken = response.IsTruncated ? response.NextContinuationToken : null;
        } while (continuationToken is not null);

        return keys;
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (IsBucketReady() || !_options.AutoCreateBucket)
        {
            return;
        }

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (IsBucketReady())
            {
                return;
            }

            try
            {
                await _s3Client.PutBucketAsync(
                    new PutBucketRequest
                    {
                        BucketName = _options.BucketName,
                        UseClientRegion = true,
                    },
                    cancellationToken);
            }
            catch (AmazonS3Exception ex) when (
                ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
                _logger.LogDebug("Bucket {Bucket} already exists.", _options.BucketName);
            }

            Interlocked.Exchange(ref _bucketReady, 1);
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    private bool IsBucketReady()
    {
        return Volatile.Read(ref _bucketReady) == 1;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Storage key is required.", nameof(key));
        }
    }
}
