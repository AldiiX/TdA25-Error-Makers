global using HCS = TdA25_Error_Makers.Services.HttpContextService;
using dotenv.net;
using MySql.Data.MySqlClient;
using StackExchange.Redis;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Middlewares;
using TdA25_Error_Makers.Services;

namespace TdA25_Error_Makers;



public static class Program {

    public static WebApplication App { get; private set; } = null!;
    public static IDictionary<string, string> ENV { get; private set; } = null!;
    public static ILogger Logger => App.Logger;

    
    #if DEBUG || TESTING
        public const bool DEVELOPMENT_MODE = true;
    #else
        public const bool DEVELOPMENT_MODE = false;
    #endif

    
    

    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

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
        builder.Services.AddSingleton<IViewRenderService, ViewRenderService>();
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
        HCS.Configure(httpContextAccessor);
        

        
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
        App.UseMiddleware<ErrorHandlingMiddleware>();
        App.UseMiddleware<BeforeInitMiddleware>();
        App.MapControllerRoute(name: "default", pattern: "/");



        // Vyzkoušení připojení k databázi
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection(Database.CONNECTION_STRING);
            conn.Open();
            Logger.Log(LogLevel.Information, $"Database connection to {Database.DATABASE_IP} successful.");
        } catch (Exception _) {
            Logger.Log(LogLevel.Error, "Database connection error, trying fallback.");

            try {
                string fallbackConnectionString = $"server=localhost;userid=tda25;password=password;database=tda25;pooling=true;Max Pool Size={Database.MAX_POOL_SIZE};";
                conn = new MySqlConnection(fallbackConnectionString);
                conn.Open();
                Logger.Log(LogLevel.Information, "Fallback database connection successful.");
                Database.CONNECTION_STRING = fallbackConnectionString;
            } catch (Exception __) {
                Logger.Log(LogLevel.Error, "Database connection error, fallback failed.");
                return;
            }

            return;
        }

        conn.Close();

        

        // Spuštění aplikace
        App.Run();
    }
}