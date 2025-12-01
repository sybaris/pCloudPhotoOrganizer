using pCloudPhotoOrganizer.Services;

namespace pCloudPhotoOrganizer.Services;
public class PCloudAuthService
{
    private const string TokenKey = "pcloud_token";

    public async Task<string> GetTokenAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(TokenKey);
            return token;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveTokenAsync(string token)
    {
        await SecureStorage.Default.SetAsync(TokenKey, token);
    }
}
