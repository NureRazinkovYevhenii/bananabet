using BananaBet_API.Models;
using BananaBet_API.Services;
using Microsoft.EntityFrameworkCore;

namespace BananaBet_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<BettingDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                )
            );

            builder.Services.AddHttpClient<FootballDataClient>();
            builder.Services.AddHttpClient<MlClient>(client =>
            {
                client.BaseAddress = new Uri(
                    builder.Configuration["MlService:BaseUrl"]!
                );
            });
            builder.Services.AddHttpClient<EloSnapshotClient>();
            builder.Services.AddScoped<BlockchainTxVerifierService>();
            builder.Services.AddScoped<StatsService>();
            builder.Services.AddScoped<MatchPipelineService>();
            builder.Services.AddScoped<BetService>();
            builder.Services.AddHostedService<MatchPipelineWorker>();
            builder.Services.AddHostedService<EloSnapshotWorker>();
            builder.Services.AddSingleton<BlockchainClient>();
            builder.Services.AddScoped<BlockchainOracleService>();
            builder.Services.AddHostedService<BlockchainOracleWorker>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); // ���� ���� / auth
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BananaBet API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowFrontend");
            app.UseAuthorization();

            // Enable static files for wwwroot (for team logos and other assets)
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
