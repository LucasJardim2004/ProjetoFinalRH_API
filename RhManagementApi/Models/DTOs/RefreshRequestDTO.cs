namespace RhManagementApi.DTOs
{
    public class RefreshRequestDTO
    {
        public string RefreshToken {get; set; }

        public string refreshToken;

        public RefreshRequestDTO(string refreshToken) 
        {
            this.refreshToken = refreshToken;    
        }
    }
}