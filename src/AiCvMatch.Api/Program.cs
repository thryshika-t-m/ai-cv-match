using AiCvMatch.Api.Extensions;
using AiCvMatch.Api.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 11 * 1024 * 1024;
});

AnalysisServiceExtensions.ApplyAnalysisApiKeys(builder.Configuration);
builder.Services.AddMatchAnalysisProvider(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("AngularDev");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
