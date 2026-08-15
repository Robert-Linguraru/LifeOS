using LifeOS.Core.Abstractions;
using LifeOS.Infrastructure.Extensions;
using Hangfire;
using LifeOS.Web.Components;
using LifeOS.Web.Options;
using LifeOS.Web.Services;
using LifeOS.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddWeb(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);


var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

ReminderJobRegistration.RegisterDueReminderJob(
    app.Services.GetRequiredService<IRecurringJobManager>());

app.Run();
