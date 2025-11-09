using Microsoft.EntityFrameworkCore;
using ReviewsParser.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Подключение к БД
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=parser.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers(); 

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();