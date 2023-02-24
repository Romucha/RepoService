using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;
using RepoService.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace RepoService.Console.Data
{
 public class RepoReceiverService
 {
  private readonly ILogger<RepoReceiverService> _logger;
  private readonly IConfiguration _configuration;

  private string _repoaddress;

  public RepoReceiverService(ILogger<RepoReceiverService> logger, IConfiguration configuration)
  {
   _configuration = configuration;
   _logger = logger;
   _repoaddress = _configuration["RepoServiceAddress"];
  }

  public async Task<IEnumerable<ProductModel>> GetProductModels(string? productName = null)
  {
   UriBuilder uriBuilder = new UriBuilder($"{_repoaddress}/api/products");
   var query = HttpUtility.ParseQueryString(string.Empty);
   if (productName != null)
   {
    query[nameof(productName)] = productName;
   }
   uriBuilder.Query = query.ToString();
   string url = uriBuilder.ToString();
   var request = new HttpRequestMessage(HttpMethod.Get, url);
   request.Headers.Add("User-Agent", "RepositoryServiceReceiver");

   var client = new HttpClient();

   var response = await client.SendAsync(request);
   if (response.IsSuccessStatusCode)
   {
    var content = await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<IEnumerable<ProductModel>>(content, new JsonSerializerOptions()
    {
     PropertyNameCaseInsensitive = true
    });
   }
   else
   {
    throw new Exception(response.StatusCode.ToString());
   }
  }

  [Obsolete]
  public async Task<FileResult> GetProductModel(Guid guid)
  {
   UriBuilder uriBuilder = new UriBuilder($"{_repoaddress}/api/products/{guid}");
   var query = HttpUtility.ParseQueryString(string.Empty); query["download"] = true.ToString();
   uriBuilder.Query = query.ToString();
   string url = uriBuilder.ToString();
   var request = new HttpRequestMessage(HttpMethod.Get, url);
   request.Headers.Add("User-Agent", "RepositoryServiceReceiver");

   var client = new HttpClient();

   var response = await client.SendAsync(request);

   if (response.IsSuccessStatusCode)
   {
    return new FileContentResult(await response.Content.ReadAsByteArrayAsync(), "application/octet-stream");
   }
   else
   {
    throw new Exception(response.StatusCode.ToString());
   }
  }

  public async Task<bool> DeleteProduct(Guid guid)
  {
   UriBuilder uriBuilder = new UriBuilder($"{_repoaddress}/api/products/{guid}");
   string url = uriBuilder.ToString();
   var client = new HttpClient();

   var response = await client.DeleteAsync(url);

   if (response.IsSuccessStatusCode)
   {
    return true;
   }
   else
   {
    return false;
   }
  }

  public async Task UploadFiles(IEnumerable<IBrowserFile> files)
  {
   UriBuilder uriBuilder = new UriBuilder($"{_repoaddress}/api/products");
   string url = uriBuilder.ToString();
   var client = new HttpClient();

   using (var content = new MultipartFormDataContent())
   {
    foreach (var file in files)
    {
     content.Add(CreateFileContent(file.OpenReadStream(10485760), file.Name, "application/octet-stream"));
    }

    var response = await client.PostAsync(url, content);
    response.EnsureSuccessStatusCode();
   }
  }

  private StreamContent CreateFileContent(Stream stream, string fileName, string contentType)
  {
   var fileContent = new StreamContent(stream);
   fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
   {
    Name = "\"files\"",
    FileName = "\"" + fileName + "\""
   }; // the extra quotes are key here
   fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
   return fileContent;
  }
 }
}
