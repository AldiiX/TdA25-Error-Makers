global using HCS = TdA25_Error_Makers.Services.HttpContextService;
using dotenv.net;
using StackExchange.Redis;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Middlewares;
using TdA25_Error_Makers.Services;

namespace TdA25_Error_Makers;



public static class Program {

    public static Microsoft.AspNetCore.Builder.WebApplication App { get; private set; } = null!;
    public static IDictionary<string, string> ENV { get; private set; } = null!;
    public static ILogger Logger => App.Logger;

    
    #if DEBUG || TESTING
        public const bool DEVELOPMENT_MODE = true;
    #else
        public const bool DEVELOPMENT_MODE = false;
    #endif

    
    

    public static void Main(string[] args) {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.AddStackExchangeRedisCache(options => {
            if (DEVELOPMENT_MODE) {
                options.ConfigurationOptions = new ConfigurationOptions {
                    EndPoints = { $"{ENV["DATABASE_IP"]}:{ENV["REDIS_PORT"]}" },
                    Password = ENV["REDIS_PASSWORD"],
                };
            } else options.Configuration = "localhost:6379";

            options.InstanceName = "TdA25_Error_Makers_Session";
        });
        builder.Services.AddSession(options => {
            options.IdleTimeout = TimeSpan.FromDays(365);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;

            options.Cookie.MaxAge = TimeSpan.FromDays(365); // Trvání cookie na 365 dní
            //options.Cookie.Expiration = TimeSpan.FromDays(365);
            options.Cookie.Name = "tda25_error_makers_session";
        });

        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        #if DEBUG
                builder.Configuration.AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
        #elif RELEASE
                builder.Configuration.AddJsonFile("appsettings.Release.json", optional: true, reloadOnChange: true);
        #elif TESTING
            builder.Configuration.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: true);
        #endif

        builder.Configuration.AddEnvironmentVariables();

        App = builder.Build();
        ENV = DotEnv.Read();
        
        
        
        // Konfigurace HttpContextService
        var httpContextAccessor = App.Services.GetRequiredService<IHttpContextAccessor>();
        HttpContextService.Configure(httpContextAccessor);
        

        
        // Configure the HTTP request pipeline.
        if (!App.Environment.IsDevelopment()) {
            App.UseExceptionHandler("/error");
            App.UseStatusCodePagesWithReExecute("/error/{0}");
            App.UseHsts();
        }

        
        
        App.UseHttpsRedirection();
        App.UseStaticFiles();
        App.UseSession();
        App.UseRouting();
        App.UseAuthorization();
        //App.UseMiddleware<ErrorHandlingMiddleware>();
        App.MapControllerRoute(name: "default", pattern: "/");



        // Vyzkoušení připojení k databázi
        using var conn = Database.GetConnection(false);
        if (conn == null) Logger.Log(LogLevel.Critical, $"Database connection ({Database.DATABASE_IP}) error při spouštění aplikace.");
        else Logger.Log(LogLevel.Information, $"Database connection ({Database.DATABASE_IP}) successful při spouštění aplikace.");
        conn?.Close();

        

        // Spuštění aplikace
        App.Run();
    }
}