namespace PetSitterApp.Services;

/// <summary>
/// The signed-in session could not produce a Google access token, so the user
/// has to sign in again. A dedicated type means callers no longer have to
/// match on an exception message to detect it.
/// </summary>
public class AccessTokenUnavailableException : Exception
{
    public AccessTokenUnavailableException()
        : base("Could not retrieve access token")
    {
    }
}
