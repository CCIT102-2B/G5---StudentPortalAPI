var builder = WebApplication.CreateBuilder(args);

// Account API uses port 5002, so this one uses 5003.
builder.WebHost.UseUrls("http://localhost:5003");

builder.Services.AddControllers();

// Allow the frontend (VS Code Live Server) to call this API from the browser.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();