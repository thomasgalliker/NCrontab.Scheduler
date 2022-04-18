using NCrontab.Scheduler;
using NCrontab.Scheduler.AspNetCore.Extensions;
using NCrontab.Scheduler.AspNetCoreSample.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add scheduler
builder.Services.AddScheduler();

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
