using MongoDB.Driver;
using Library;

var builder = WebApplication.CreateBuilder(args);

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB");
var mongoDatabaseName = "CaseSecilStoreDb";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));
builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});

builder.Services.AddSingleton<ConfigurationReader>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var configSection = configuration.GetSection("ConfigurationReader");
    var applicationName = configSection["ApplicationName"];
    var refreshInterval = int.Parse(configSection["RefreshIntervalMs"] ?? "30000");

    return new ConfigurationReader(applicationName, mongoConnectionString, refreshInterval);
});

builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var configReader = scope.ServiceProvider.GetRequiredService<ConfigurationReader>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    await configReader.SyncConfigurationsFromAppSettings(configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();