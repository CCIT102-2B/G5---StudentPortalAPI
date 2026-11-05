using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;

var builder = WebApplication.CreateBuilder(args);

// Uses the shared database. Connection string comes from the ConnectionStrings__Portal environment variable.
builder.Services.AddDbContext<PortalDbContext>(options => PortalDatabase.Configure(options));
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("Portal", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    PortalDatabase.Initialize(scope.ServiceProvider.GetRequiredService<PortalDbContext>());

app.UseCors("Portal");
app.MapControllers();
app.Run("http://localhost:5002");