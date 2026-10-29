var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("Portal", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors("Portal");
app.MapControllers();
app.Run("http://localhost:5002");
