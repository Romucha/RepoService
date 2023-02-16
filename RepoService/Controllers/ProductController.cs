using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using RepoService.Models;
using System;
using System.Collections;
using System.Data.Entity;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RepoService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger;

        private readonly IConfiguration _configuration;

        public ProductsController(ILogger<ProductsController> logger, IConfiguration configuration)
        {
            this._logger = logger;
            this._configuration = configuration;
        }

        [HttpGet]
        public async IAsyncEnumerable<ProductModel?> Get()
        {
            string repoDir = _configuration["RepoStorage"]; 
            string productsDir = Path.Combine(repoDir, "Products");
            if (string.IsNullOrEmpty(repoDir)
                || !Directory.Exists(repoDir)
                || !Directory.Exists(productsDir))
            {
                yield break;
            }
            var jsonFiles = Directory.EnumerateFiles(productsDir, "*.json", SearchOption.AllDirectories);
            foreach (var jsonFile in jsonFiles)
            {
                ProductModel? model = null;
                try
                {
                    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                    using (FileStream fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.Read))                    
                    {
                        model = await JsonSerializer.DeserializeAsync<ProductModel>(fs);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                yield return model;
            }
        }
    }
}
