using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using web_api_users.Domain.Interfaces;

namespace web_api_users.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [SwaggerTag("Gestión de buckets y objetos dentro de MinIO.")]
    public class BucketController : ControllerBase
    {
        private readonly IBucketService _bucketService;

        public BucketController(IBucketService bucketService)
        {
            _bucketService = bucketService;
        }

        /// <summary>
        /// Crea un nuevo bucket en MinIO.
        /// </summary>
        [Authorize(Policy = "MinioBucketCreate")]
        [HttpPost("CreateBucketMINio")]
        [SwaggerOperation(
            Summary = "Crear bucket",
            Description = "Crea un nuevo bucket en MinIO con el nombre proporcionado."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Bucket creado exitosamente.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "El bucket ya existe o hubo un error al crearlo.")]
        public async Task<IActionResult> CreateBucketMINio(
            [FromQuery, SwaggerParameter("Nombre del bucket a crear. Ej: 'documentos-usuarios'", Required = true)]
            string name = "documentos-usuarios")
        {
            var result = await _bucketService.CreateBucket(name);

            if (!result.Success)
                return Conflict(result.Message);

            return Ok(result.Message);
        }

        /// <summary>
        /// Lista todos los buckets existentes en MinIO.
        /// </summary>
        [Authorize(Policy = "MinioBucketList")]
        [HttpGet("ListBucketsMINio")]
        [SwaggerOperation(
            Summary = "Listar buckets",
            Description = "Obtiene una lista de todos los buckets existentes en MinIO."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de buckets obtenida exitosamente.")]
        public async Task<IActionResult> ListBucketsMINio()
        {
            var buckets = await _bucketService.ListBuckets();
            return Ok(buckets);
        }

        /// <summary>
        /// Lista los objetos dentro de un bucket.
        /// </summary>
        [Authorize(Policy = "MinioBucketListObject")]
        [HttpGet("ListObjectsMINio")]
        [SwaggerOperation(
            Summary = "Listar objetos en bucket",
            Description = "Obtiene la lista de archivos (objetos) dentro del bucket especificado."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de objetos obtenida exitosamente.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Error al listar los objetos del bucket.")]
        public async Task<IActionResult> ListObjectsMINio(
            [FromQuery, SwaggerParameter("Nombre del bucket. Ej: 'documentos-usuarios'", Required = true)]
            string name = "documentos-usuarios")
        {
            var result = await _bucketService.ListObjects(name);

            if (!result.Success)
                return Conflict(result.Message);

            return Ok(result.Objects);
        }

        /// <summary>
        /// Elimina un bucket de MinIO.
        /// </summary>
        [Authorize(Policy = "MinioBucketDelete")]
        [HttpDelete("DeleteBucketMINio")]
        [SwaggerOperation(
            Summary = "Eliminar bucket",
            Description = "Elimina un bucket de MinIO. El bucket debe estar vacío."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Bucket eliminado exitosamente.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Error al eliminar el bucket.")]
        public async Task<IActionResult> DeleteBucketMINio(
            [FromQuery, SwaggerParameter("Nombre del bucket a eliminar. Ej: 'documentos-usuarios'", Required = true)]
            string name = "documentos-usuarios")
        {
            var result = await _bucketService.DeleteBucket(name);

            if (!result.Success)
                return Conflict(result.Message);

            return Ok(result.Message);
        }
    }
}
