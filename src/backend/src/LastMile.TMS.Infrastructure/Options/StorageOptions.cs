namespace LastMile.TMS.Infrastructure.Options;

public sealed class StorageOptions
{
    public string Endpoint { get; set; } = "http://localhost:9000";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Region { get; set; } = "us-east-1";

    public string BucketName { get; set; } = "lastmile-binaries";

    public bool ForcePathStyle { get; set; } = true;

    public bool AutoCreateBucket { get; set; } = true;
}
