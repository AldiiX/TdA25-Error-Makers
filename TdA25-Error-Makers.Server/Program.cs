using dotenv.net;
using MySql.Data.MySqlClient;
using StackExchange.Redis;
using TdA25_Error_Makers.Server.Classes;
using TdA25_Error_Makers.Server.Middlewares;
using WebSocketMiddleware = TdA25_Error_Makers.Server.Middlewares.WebSocketMiddleware;

namespace TdA25_Error_Makers.Server;

public static class Program {

    public static DateTime AppStartTime { get; } = DateTime.Now;
    public static WebApplication App { get; private set; } = null!;
    public static IDictionary<string, string> ENV { get; private set; } = null!;
    public static ILogger Logger => App.Logger;
    public static TimeSpan AppUptime => DateTime.Now - AppStartTime;

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
            options.Cookie.Name = "SESSION";
        });
        //builder.Services.AddSingleton<IViewRenderService, ViewRenderService>();
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        builder.Host.ConfigureAppConfiguration((hostingContext, config) => {
            config.Sources.OfType<FileConfigurationSource>().ToList().ForEach(source =>
                source.ReloadOnChange = false);
        });

        #if DEBUG
            builder.Configuration.AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
        #elif RELEASE
            builder.Configuration.AddJsonFile("appsettings.Release.json", optional: true, reloadOnChange: false);
        #elif TESTING
            builder.Configuration.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: false);
        #elif RELEASE_UBUNTU
            builder.Configuration.AddJsonFile("appsettings.ReleaseUbuntu.json", optional: true, reloadOnChange: false);
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



        //App.UseHttpsRedirection();
        App.UseSession();
        App.UseWebSockets(new WebSocketOptions() {
            KeepAliveInterval = TimeSpan.FromSeconds(120),
        });
        App.UseMiddleware<BeforeInitMiddleware>();
        App.UseMiddleware<WebSocketMiddleware>();
        //App.UseStaticFiles();
        App.UseRouting();
        App.UseAuthorization();
        //App.UseMiddleware<ErrorHandlingMiddleware>();
        App.MapControllerRoute(name: "default", pattern: "/");



        // test připojení k databázi, pokud selže, zkusí se fallback
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection(Database.CONNECTION_STRING);
            conn.Open();
            Logger.Log(LogLevel.Information, $"Database connection to {Database.DATABASE_IP} successful.");
        } catch (Exception e) {
            Logger.Log(LogLevel.Error, $"Database connection „{conn?.DataSource}” error: {e.Message}, trying fallback.");

            try {
                Database.SwitchToFallbackServer();
                conn = new MySqlConnection(Database.CONNECTION_STRING);
                conn.Open();
                Logger.Log(LogLevel.Information, "Fallback database connection successful.");
            } catch (Exception e2) {
                Logger.Log(LogLevel.Error, $"Database connection „{conn?.DataSource}” error: {e2.Message}, fallback failed.");
                Database.SwitchToNormalServer();
                Database.LAST_CONNECTION_FAILED = true;
            }

            Database.LAST_CONNECTION_FAILED = true;
        }

        conn?.Close();



        // Spuštění aplikace
        App.Run();
    }
}