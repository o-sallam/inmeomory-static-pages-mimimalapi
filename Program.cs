using StaticAotWeb.Services;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/{page?}", async (string? page, HttpContext context) =>
{
    var htmlPageService = new HtmlPageService();
    var enumerator = htmlPageService.GetPageHtmlChunksAsync(page);
    context.Response.ContentType = "text/html";
    await foreach (var chunk in enumerator)
    {
        await context.Response.WriteAsync(chunk);
        await context.Response.Body.FlushAsync();
    }
});

app.MapGet("/partial-content", () =>
    TypedResults.Content("<div>محتوى HTML هنا</div>", "text/html"));

app.Run();