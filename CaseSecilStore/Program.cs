using MongoDB.Driver;
var builder = WebApplication.CreateBuilder(args);
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB");
var mongoDatabaseName = "CaseSecilStoreDb";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));
builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});
// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
