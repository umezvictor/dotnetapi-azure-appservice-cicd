namespace ImageResizerAPI;

internal sealed class S3Settings
{
    public required string BucketName { get; init; }
    public required string Region { get; init; }
}
