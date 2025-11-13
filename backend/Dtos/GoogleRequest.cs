namespace Backend.Dtos
{
    public class GoogleLoginRequest
    {
        /// <summary>
        /// JWT token from Google (with or without 'Bearer ' prefix)
        /// </summary>
        /// <example>Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2FjY291bnRzLmdvb2dsZS5jb20iLCJhenAiOiI...</example>
        public required string JwtToken { get; set; }
    }
}