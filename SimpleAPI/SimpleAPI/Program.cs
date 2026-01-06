
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace BasicScoreboardAPI
{
    class Program
    {
        private static readonly ConcurrentDictionary<string, int> Leaderboard = new(StringComparer.OrdinalIgnoreCase);
        private static List<ScoreEntry> sortedScores = new List<ScoreEntry>();
        private static readonly object SortedLock = new();
        
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
            app.MapGet("/time", SayTime);
            app.MapPost("/leaderboard", UpsertScore);
            app.MapGet("/leaderboard/score/{name}", GetScore);
            app.MapGet("/leaderboard/sortByName", ReturnScoreboardByName);
            app.MapGet("/leaderboard/sortByScore", ReturnScoreboardByScore);
            
            if(app.Environment.IsDevelopment()) app.MapGet("/leaderboard/dump", () => { return Results.Ok(Leaderboard);});
            
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
        public record ScoreEntry(string Name, int Score);
        public record ScoreDto(string Name, int Score);

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
            sortedScores = ReturnOrderedList();
            
            
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

        public sealed record OutputScore(string name, int score);
        
        private static IResult GetScore(string name)
        {
            return Leaderboard.TryGetValue(name, out int score) ? 
                Results.Ok(new OutputScore(name, score)) : 
                Results.NotFound(new {Name = name});
        }

        private static IResult ReturnScoreboardByScore()
        {
            return Results.Ok(sortedScores);
        }
        
        private static IResult ReturnScoreboardByName()
        {
            var snapshot = Leaderboard;
            var sorted = snapshot
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new
                {
                    name = kvp.Key,
                    score = kvp.Value
                })
                .ToList();
            return Results.Ok(sorted);
        }

        private static List<ScoreEntry> ReturnOrderedList()
        {
            var snapshot = Leaderboard;
            var sorted = snapshot
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => new ScoreEntry(kvp.Key, kvp.Value))
                .ToList();

            return sorted;
        }
    }
}
