using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace ResumeSpy.UI.Swagger
{
    /// <summary>
    /// A custom Swagger document filter to ensure that request bodies are properly
    /// represented with individual fields in the Swagger UI, avoiding raw JSON strings.
    /// </summary>
    public class FormDataDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var path in swaggerDoc.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    if (operation.RequestBody == null) continue;

                    // We are targeting application/json, as this is the default for [FromBody]
                    var jsonContent = operation.RequestBody.Content
                        .FirstOrDefault(c => c.Key.Equals("application/json", System.StringComparison.OrdinalIgnoreCase));

                    if (jsonContent.Value?.Schema == null) continue;

                    // Create a new media type for form data
                    var newContent = new OpenApiMediaType
                    {
                        Schema = jsonContent.Value.Schema
                    };

                    // Replace the original JSON content with a form-urlencoded one
                    operation.RequestBody.Content.Clear();
                    operation.RequestBody.Content.Add("application/x-www-form-urlencoded", newContent);
                }
            }
        }
    }
}
