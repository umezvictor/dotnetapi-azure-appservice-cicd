using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImageResizerAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {

        private readonly BlobServiceClient _blobServiceClient;
        private readonly ProcessedFileDbContext _processedFileDbContext;
        private readonly string _containerName;


        public ImageController(BlobServiceClient blobServiceClient, IConfiguration configuration,
            ProcessedFileDbContext processedFileDbContext)
        {
            _blobServiceClient = blobServiceClient;
            _processedFileDbContext = processedFileDbContext;
            _containerName = configuration.GetSection("AzureBlobStorage")["ContainerName"]!;
        }

        //upload image to Azure Blob Storage
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


        [HttpGet("health")]
        public IActionResult Test()
        {
            return Ok("API is healthy yes!");
        }
    }
}



