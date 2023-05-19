using NCrontab.Scheduler;
using NCrontab.Scheduler.AspNetCoreSample.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add scheduler using default configuration
builder.Services.AddHostedScheduler();

// Add hosted scheduler using custom configuration
//builder.Services.AddHostedScheduler(o =>
//{
//    o.DateTimeKind = DateTimeKind.Utc;
//});

// Add hosted scheduler using configuration from appSettings.json
//var configurationSection = builder.Configuration.GetSection("NCrontab.Scheduler");
//builder.Services.AddHostedScheduler(configurationSection);

// Add nightly task to scheduler
builder.Services.AddSingleton<IScheduledTask, NightlyTask>();

// Add nightly task (async) to scheduler
builder.Services.AddSingleton<IAsyncScheduledTask, NightlyAsyncTask>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
