using Microsoft.AspNetCore.Mvc;
using RepoService.Models;
using System.Text.Json;

namespace RepoService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScriptsController : Controller
    {
        private readonly ILogger<ProductsController> _logger;

        private readonly IConfiguration _configuration;

        public ScriptsController(ILogger<ProductsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async IAsyncEnumerable<ScriptModel> Get()
        {
            string repoDir = _configuration["RepoStorage"];
            string scriptsDir = Path.Combine(repoDir, "Scripts");
            if (string.IsNullOrEmpty(repoDir)
                || !Directory.Exists(repoDir)
                || !Directory.Exists(scriptsDir))
            {
                yield break;
            }
            var jsonFiles = Directory.EnumerateFiles(scriptsDir, "*.json", SearchOption.AllDirectories);
            foreach (var jsonFile in jsonFiles)
            {
                ScriptModel? model = null;
                try
                {
                    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                    using (FileStream fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        model = await JsonSerializer.DeserializeAsync<ScriptModel>(fs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                yield return model;
            }
        }
    }
}
