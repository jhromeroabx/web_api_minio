namespace web_api_users.Application.Dtos
{
    public class GetObjectData
    {
        public string bucketName { get; set; }
        public string objectName { get; set; }
        public string contentType { get; set; }
    }
}
