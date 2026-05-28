using MudBlazor.Services;

using Syncfusion.Blazor;

using Vectus.Shared.Services;
using Vectus.Web.Components;
using Vectus.Web.Services;

using VectusLibrary.DataAccess;

var builder = WebApplication.CreateBuilder(args);

SqlDataAccess.SetupConfiguration();

// Add services to the container.
builder.Services
	.AddSyncfusionBlazor()
	.AddMudServices()
	.AddRazorComponents()
	.AddInteractiveServerComponents();

// Add device-specific services used by the Vectus.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IUpdateService, UpdateService>();
builder.Services.AddSingleton<IVibrationService, VibrationService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddScoped<ISaveAndViewService, SaveAndViewService>();
builder.Services.AddScoped<ISoundService, SoundService>();
builder.Services.AddScoped<IDataStorageService, DataStorageService>();
builder.Services.AddScoped<PageRefreshState>();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	.AddAdditionalAssemblies(
		typeof(Vectus.Shared._Imports).Assembly);

app.Run();
