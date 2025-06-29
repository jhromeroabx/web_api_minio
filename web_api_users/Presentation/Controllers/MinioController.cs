using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using web_api_users.Domain.Interfaces;

namespace web_api_users.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MinioController : ControllerBase
    {
        private readonly IMinioService _minioService;


        public MinioController(IMinioService minioService)
        {
            _minioService = minioService;
        }

        [HttpPost("CreateBucketMINio")]
        public async Task<IActionResult> CreateBucketMINio(string name)
        {
            var result = await _minioService.CreateBucket(name);

            if (!result.Success)
                return Conflict(result.Message);

            return Ok(result.Message);
        }

        [HttpGet("ListBucketsMINio")]
        public async Task<IActionResult> ListBucketsMINio()
        {
            var buckets = await _minioService.ListBuckets();
            return Ok(buckets);
        }

        [HttpGet("ListObjectsMINio")]
        public async Task<IActionResult> ListObjectsMINio(string name)
        {
            var result = await _minioService.ListObjects(name);
            if (!result.Success)
                return Conflict(result.Message);
            return Ok(result.Objects);
        }

        [HttpDelete("DeleteBucketMINio")]
        public async Task<IActionResult> DeleteBucketMINio(string name)
        {
            var result = await _minioService.DeleteBucket(name);
            if (!result.Success)
                return Conflict(result.Message);
            return Ok(result.Message);
        }

        [HttpPost("CreateObjectMINio")]
        public async Task<IActionResult> CreateObjectMINio(string nameBucket, string nameObject, string contentType, IFormFile file)
        {
            var result = await _minioService.UploadImageAsync(nameBucket, nameObject, contentType, file);
            return result.Success ? Ok(result.Message) : Conflict(result.Message);
        }

        [HttpPost("CreateObjectMp3MINio")]
        public async Task<IActionResult> CreateObjectMp3MINio(string nameBucket, string nameObject, string contentType, IFormFile file)
        {
            var result = await _minioService.UploadAudioAsync(nameBucket, nameObject, contentType, file);
            return result.Success ? Ok(result.Message) : Conflict(result.Message);
        }

        [HttpGet("GetObjectMp3MINio")]
        public async Task<IActionResult> GetObjectMp3MINio(string nameBucket, string nameObject)
        {
            var result = await _minioService.GetAudioAsync(nameBucket, nameObject);
            return result.Success
                ? File(result.Data, result.ContentType)
                : Conflict(result.Message);
        }      

        [HttpGet("GetObjectMINio")]
        public async Task<IActionResult> GetObjectMINio(string nameBucket, string nameObject)
        {
            var result = await _minioService.GetObjectAsync(nameBucket, nameObject);
            return result.Success
                ? File(result.Data, result.ContentType)
                : Conflict(result.Message);
        }

        [HttpDelete("DeleteObjectMINio")]
        public async Task<IActionResult> DeleteObjectMINio(string bucket, string objectname)
        {
            var result = await _minioService.DeleteObjectAsync(bucket, objectname);
            return result.Success
                ? Ok(result.Message)
                : Conflict(result.Message);
        }
    }
}
