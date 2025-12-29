using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Add text/plain input formatter for string bodies
    options.InputFormatters.Insert(0, new TextPlainInputFormatter());
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Redis connection
var redisConfig = builder.Configuration.GetValue<string>("Redis:Configuration");
Console.WriteLine($"[STARTUP] Redis:Configuration = '{redisConfig}'");

if (!string.IsNullOrEmpty(redisConfig))
{
    try {
      Console.WriteLine($"[STARTUP] Attempting to connect to Redis at {redisConfig}");
      var mux = ConnectionMultiplexer.Connect(redisConfig);
      builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
      Console.WriteLine("[STARTUP] Redis connection successful, IConnectionMultiplexer registered");
    } catch (Exception ex) {
      Console.Error.WriteLine($"[STARTUP] Redis connect failed: {ex}");
      throw;
    }
}
else
{
    Console.WriteLine("[STARTUP] WARNING: Redis:Configuration is empty/null");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}
else
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("[STARTUP] Application ready to handle requests");
app.Run();

// Custom input formatter for text/plain
internal class TextPlainInputFormatter : TextInputFormatter
{
    public TextPlainInputFormatter()
    {
        SupportedMediaTypes.Add("text/plain");
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        var request = context.HttpContext.Request;
        using var reader = new StreamReader(request.Body, encoding);
        var body = await reader.ReadToEndAsync();
        return InputFormatterResult.Success(body);
    }
}