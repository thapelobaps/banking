using Kape.Api.Configuration;
using Kape.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKapeApi(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseKapeCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
