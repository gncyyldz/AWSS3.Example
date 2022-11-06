using Amazon.S3;
using Amazon.S3.Model;
using AWSS3.Example.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AWSS3.Example.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        readonly IAmazonS3 _amazonS3;

        public FilesController(IAmazonS3 amazonS3)
        {
            _amazonS3 = amazonS3;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFilesAsync(string bucketName, string? prefix)
        {
            bool bucketExists = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExists)
                return NotFound($"Bucket {bucketName} does not exist.");
            ListObjectsV2Request request = new()
            {
                BucketName = bucketName,
                Prefix = prefix
            };
            ListObjectsV2Response response = await _amazonS3.ListObjectsV2Async(request);
            List<S3ObjectDTO> objectDatas = response.S3Objects.Select(@object =>
             {
                 GetPreSignedUrlRequest urlRequest = new()
                 {
                     BucketName = bucketName,
                     Key = @object.Key,
                     Expires = DateTime.UtcNow.AddMinutes(1)
                 };
                 return new S3ObjectDTO
                 {
                     Name = @object.Key,
                     Url = _amazonS3.GetPreSignedURL(urlRequest)
                 };
             }).ToList();
            return Ok(objectDatas);
        }

        [HttpGet("download/{bucketName}/{fileName}")]
        public async Task<IActionResult> GetFileByNameAsync(string bucketName, string fileName)
        {
            bool bucketExists = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExists)
                return NotFound($"Bucket {bucketName} does not exist.");
            GetObjectResponse response = await _amazonS3.GetObjectAsync(bucketName, fileName);
            return File(response.ResponseStream, response.Headers.ContentType);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFileAsync(IFormFile file, string bucketName, string? prefix)
        {
            bool bucketExists = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExists)
                return NotFound($"Bucket {bucketName} does not exist.");
            PutObjectRequest request = new()
            {
                BucketName = bucketName,
                Key = String.IsNullOrEmpty(prefix) ? file.FileName : $"{prefix?.TrimEnd('/')}/{file.FileName}",
                InputStream = file.OpenReadStream()
            };
            request.Metadata.Add("Content-Type", file.ContentType);
            await _amazonS3.PutObjectAsync(request);
            return Ok($"File {prefix}/{file.FileName} uploaded to S3 successfully!");
        }

        [HttpDelete("{bucketName}/{fileName}")]
        public async Task<IActionResult> DeleteFileAsync(string bucketName, string fileName)
        {
            bool bucketExists = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExists)
                return NotFound($"Bucket {bucketName} does not exist.");
            await _amazonS3.DeleteObjectAsync(bucketName, fileName);
            return NoContent();
        }
    }
}
