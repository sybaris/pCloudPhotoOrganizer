using pCloudPhotoOrganizer.Models;
using System.Net.Http.Headers;

namespace pCloudPhotoOrganizer.Services;
public class PCloudFileService
{
    private readonly HttpClient _client = new HttpClient();

    public async Task<long> CreateFolder(string token, string parentId, string name)
    {
        var url = $"https://api.pcloud.com/createfolder?access_token={token}&name={name}&parentfolderid={parentId}";
        var json = await _client.GetStringAsync(url);

        var obj = System.Text.Json.JsonDocument.Parse(json);
        return obj.RootElement.GetProperty("metadata").GetProperty("folderid").GetInt64();
    }

    public async Task Upload(string token, long folderId, MediaItem item)
    {
        var url = $"https://api.pcloud.com/uploadfile?access_token={token}&folderid={folderId}";

        var content = new MultipartFormDataContent();
        var stream = File.OpenRead(item.FilePath);

        content.Add(new StreamContent(stream)
        {
            Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") }
        }, "file", Path.GetFileName(item.FilePath));

        var response = await _client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
    }
}
