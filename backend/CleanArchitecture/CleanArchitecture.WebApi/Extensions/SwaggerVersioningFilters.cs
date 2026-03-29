using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace CleanArchitecture.WebApi.Extensions
{
    public class SwaggerDefaultVersionPathFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = swaggerDoc.Paths.ToDictionary(path => path.Key.Replace("v{version}", "v1"), path => path.Value);
            swaggerDoc.Paths = new OpenApiPaths();

            foreach (var path in paths)
            {
                swaggerDoc.Paths.Add(path.Key, path.Value);
            }
        }
    }

    public class RemoveVersionParameterFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                return;
            }

            var versionParameter = operation.Parameters.FirstOrDefault(p => p.Name == "version");
            if (versionParameter != null)
            {
                operation.Parameters.Remove(versionParameter);
            }

            var legacyPageNumber = operation.Parameters.FirstOrDefault(p => p.Name == "pageNumber");
            var canonicalPage = operation.Parameters.FirstOrDefault(p => p.Name == "page");
            if (legacyPageNumber != null && canonicalPage != null)
            {
                operation.Parameters.Remove(legacyPageNumber);
            }
        }
    }
}
