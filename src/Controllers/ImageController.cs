using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImageResizerAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {

        //private const string S3BucketName = "uploads-app-bucket";
        //private readonly IAmazonS3 _s3Client;

        private readonly BlobServiceClient _blobServiceClient;
        private readonly ProcessedFileDbContext _processedFileDbContext;
        private readonly string _containerName;


        public ImageController(BlobServiceClient blobServiceClient, IConfiguration configuration,
            ProcessedFileDbContext processedFileDbContext)
        {
            //_s3Client = s3Client;
            _blobServiceClient = blobServiceClient;
            _processedFileDbContext = processedFileDbContext;
            _containerName = configuration.GetSection("AzureBlobStorage")["ContainerName"]!;
        }

        //public ImageController(IAmazonS3 s3Client, BlobServiceClient blobServiceClient, IConfiguration configuration)
        //{
        //    _s3Client = s3Client;
        //    _blobServiceClient = blobServiceClient;
        //    _containerName = configuration.GetSection("AzureBlobStorage")["ContainerName"]!;
        //}

        //[HttpPost("upload/s3")]
        //public async Task<IActionResult> UploadImage([FromForm] FileUploadVM vm)
        //{

        //    if (vm.File.Length == 0)
        //        return BadRequest("No file uploaded.");

        //    using var stream = vm.File.OpenReadStream();
        //    var key = Guid.NewGuid();
        //    var putRequest = new PutObjectRequest
        //    {
        //        BucketName = S3BucketName,
        //        Key = $"images/{key}",
        //        InputStream = stream,
        //        ContentType = vm.File.ContentType
        //    };

        //    await _s3Client.PutObjectAsync(putRequest);
        //    return Ok(new { Message = "File uploaded to S3!", S3Key = key });

        //}



        [HttpPost("upload/azure")]
        public async Task<IActionResult> UploadImagev3([FromForm] FileUploadVM vm)
        {
            if (vm.File == null || vm.File.Length == 0)
                return BadRequest("No file uploaded.");

            var extension = Path.GetExtension(vm.File.FileName);
            var key = Guid.NewGuid().ToString();
            var blobName = $"{key}{extension}";

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobName);
            using (var stream = vm.File.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = vm.File.ContentType });
            }

            var blobUri = blobClient.Uri.ToString();
            //await _processedFileDbContext.ProcessedFiles.AddAsync(new ProcessedFile
            //{
            //    DateProcessed = DateTime.UtcNow,
            //    UserId = Guid.NewGuid().ToString() //dummy user id
            //});
            //await _processedFileDbContext.SaveChangesAsync();
            return Ok(new { Message = "File uploaded to Azure Blob Storage!", BlobUrl = blobUri, BlobName = blobName });
        }

        //[HttpGet("s3/download/{key}")]
        //public async Task<IActionResult> Download(string key)
        //{
        //    var getRequest = new GetObjectRequest
        //    {
        //        BucketName = S3BucketName,
        //        Key = $"images/{key}",
        //    };
        //    var response = await _s3Client.GetObjectAsync(getRequest);
        //    return File(response.ResponseStream, response.Headers.ContentType);
        //}


        [HttpGet("azure/download/{blobName}")]
        public async Task<IActionResult> DownloadImage(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            if (!(await blobClient.ExistsAsync()))
                return NotFound("File not found.");

            BlobDownloadResult download = await blobClient.DownloadContentAsync();

            var contentType = download.Details.ContentType ?? "application/octet-stream";
            var fileName = Path.GetFileName(blobName);
            var bytes = download.Content.ToArray();

            return File(bytes, contentType, fileName);
        }
    }
}



