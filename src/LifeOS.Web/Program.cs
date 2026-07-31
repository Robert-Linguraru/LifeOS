using LifeOS.Core.Abstractions;
using LifeOS.Infrastructure.Extensions;
using LifeOS.Web.Components;
using LifeOS.Web.Options;
using LifeOS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<DevelopmentUserOptions>(
    builder.Configuration.GetSection(
        DevelopmentUserOptions.SectionName));

builder.Services.AddScoped<ICurrentUserService,
    DevelopmentCurrentUserService>();

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

app.Run();
