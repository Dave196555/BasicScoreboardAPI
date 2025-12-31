
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace BasicScoreboardAPI
{
    class Program
    {
        private static readonly ConcurrentDictionary<string, int> Leaderboard = new(StringComparer.OrdinalIgnoreCase);
        
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapGet("/", SayHello);
            app.MapGet("/Time", SayTime);
            app.MapPost("/leaderboard", UpsertScore);
            app.MapGet("/leaderboard/{name}", GetScore);
            
            app.Run();
        }

        private static string SayHello()
        {
            return "Hello World!";
        }

        private static string SayTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        public record ScorePost(string Name, int Score);

        private static IResult UpsertScore([FromBody] ScorePost post)
        {
            if (post.Name.Length > 64)
            {
                DebugLog($"Tried to update name {post.Name} but it was too long! It's length was {post.Name.Length}.");
                return Results.BadRequest();
            }
            
            bool existed = Leaderboard.ContainsKey(post.Name);

            Leaderboard.AddOrUpdate(
                post.Name,
                addValue: post.Score,
                updateValueFactory: (_, oldScore) => Math.Max(oldScore, post.Score)
                );
            
            DebugLog($"Trying to update or set Name \"{post.Name}\" with value \"{post.Score}\".");
            
            IResult result = existed ? 
                Results.Ok(new { post.Name, post.Score, Updated = true }) : 
                Results.Created($"/leaderboard/{post.Name}", new {post.Name, post.Score, Created = true});
            
            return result;
        }

        private static void DebugLog(string inString)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]: " + inString);
        }
        
        private static IResult GetScore(string name)
        {
            return Leaderboard.TryGetValue(name, out int score) ? 
                Results.Ok(new {Name = name, Score = score}) : 
                Results.NotFound(new {Name = name});
        }
    }
}
