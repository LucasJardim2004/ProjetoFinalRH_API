
namespace RhManagementApi.DTOs
{
    public class TokenResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public TokenResponseDTO(string accessToken, string refreshToken)
        {
            AccessToken  = accessToken;   // set properties
            RefreshToken = refreshToken;
        }
    }
}