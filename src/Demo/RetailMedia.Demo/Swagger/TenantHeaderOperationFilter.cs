using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RetailMedia.Demo.Swagger;

internal sealed class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Enum =
                [
                    new Microsoft.OpenApi.Any.OpenApiString("tenant-alpha"),
                    new Microsoft.OpenApi.Any.OpenApiString("tenant-beta")
                ],
                Default = new Microsoft.OpenApi.Any.OpenApiString("tenant-alpha")
            },
            Description = "The tenant identifier. All data is scoped to this value."
        });
    }
}
