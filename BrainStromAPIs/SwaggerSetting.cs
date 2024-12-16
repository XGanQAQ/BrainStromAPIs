using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BrainStromAPIs
{
    public class AddCustomHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 添加自定义请求头
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Custom-Header", // 自定义头的名称
                In = ParameterLocation.Header,
                Description = "This is a custom header for testing purposes.",
                Required = false // 设置为 false，如果是必需的可以设置为 true
            });
        }
    }

}
