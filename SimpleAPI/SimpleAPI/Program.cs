using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace BasicScoreboardAPI
{
    class Program
    {
        private static readonly ConcurrentDictionary<string, int> Leaderboard = new(StringComparer.OrdinalIgnoreCase);
        private static List<ScoreEntry> sortedScores = new List<ScoreEntry>();
        private static DateTime startupTime;

        #region debugVariables

        private static int succesfulPostsSinceStartup = 0;
        private static int failedPostsSinceStartup = 0;
        private static int scoreRetrievalsSinceStartup = 0;
        private static int scoreboardRetrievalsSinceStartup = 0;

        #endregion
        
        
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
                
                app.MapGet("/leaderboard/dump", () => { return Results.Ok(Leaderboard);});
                app.MapGet("/debugStats", SayDeveloperStats);
            }

            app.MapGet("/", () => Results.Ok(new
            {
                name = "Simple Scoreboard API",
                status = "running",
                uptime = GetUpTime()
            }));
            app.MapGet("/uptime", SayUpTime);
            app.MapPost("/leaderboard", UpsertScore);
            app.MapGet("/leaderboard/score/{name}", GetScore);
            app.MapGet("/leaderboard/sortByName", ReturnScoreboardByName);
            app.MapGet("/leaderboard/sortByScore", ReturnScoreboardByScore);

            startupTime = DateTime.Now;
            app.Run();
        }

        #region Basic Functions

        /// <summary>
        /// Returns Developer statistics collected since the API was started.
        /// </summary>
        /// <returns>Returns an IResult. In this case IResult.Ok containing relevant developer debug data.</returns>
        private static IResult SayDeveloperStats()
        {
            return Results.Ok(new
            {
                startTime = startupTime.ToString(),
                upTime = GetUpTime(),
                scoreEntryCount = sortedScores.Count,
                postsSinceStartup = succesfulPostsSinceStartup,
                invalidPostsSinceStartup = failedPostsSinceStartup,
                scoreboardRetrievalsSinceStartup = scoreRetrievalsSinceStartup
            });
        }
        
        /// <summary>
        /// Returns the uptime of the API.
        /// </summary>
        private static IResult SayUpTime()
        {
            return Results.Ok($"Server Uptime: {GetUpTime()}");
        }
        
        /// <summary>
        /// Returns a string of the current UpTime in Json format.
        /// </summary>
        private static string GetUpTime()
        {
            TimeSpan upTime = DateTime.Now - startupTime;
            return "{" + $"{upTime.Days},{upTime.Hours},{upTime.Minutes},{upTime.Seconds}" + "}";
        }
        
        #endregion
        
        #region ScoreboardFunctions
        
        public record ScorePost(string Name, int Score);
        public record ScoreEntry(string Name, int Score);
        public record ScoreDto(string Name, int Score);

        /// <summary>
        /// Used to Post a score.
        /// </summary>
        /// <param name="post">The score to post. Contains a name and a score.</param>
        /// <returns>
        /// 400 Bad Request if the name is longer than 64 characters.
        /// 201 Created If a new score was created.
        /// 200 OK If the name already existed and the score was updated.
        /// </returns>
        private static IResult UpsertScore([FromBody] ScorePost post, ILogger<Program> logger)
        {
            if (post.Name.Length > 64)
            {
                logger.LogWarning($"Tried to update name {post.Name} but it was too long! It's length was {post.Name.Length}.",
                    post.Name,
                    post.Score);
                
                failedPostsSinceStartup++;
                return Results.BadRequest();
            }
            
            bool existed = Leaderboard.ContainsKey(post.Name);

            Leaderboard.AddOrUpdate(
                post.Name,
                addValue: post.Score,
                updateValueFactory: (_, oldScore) => Math.Max(oldScore, post.Score)
                );
            
            sortedScores = ReturnOrderedList();
            
            logger.LogInformation($"Received Request to Update or set Name \"{post.Name}\" with value \"{post.Score}\".",
                post.Name, 
                post.Score);
            
            IResult result = existed ? 
                Results.Ok(new { post.Name, post.Score, Updated = true }) : 
                Results.Created($"/leaderboard/{post.Name}", new {post.Name, post.Score, Created = true});

            succesfulPostsSinceStartup++;
            
            return result;
        }

        public sealed record OutputScore(string name, int score);
        
        /// <summary>
        /// Returns the score of the given name.
        /// </summary>
        /// <param name="name">The name used to look for the score.</param>
        /// <returns>
        /// 200 OK If the score existed and returns the score.
        /// 404 Not Found If the name is not in the list. 
        /// </returns>
        private static IResult GetScore(string name)
        {
            scoreRetrievalsSinceStartup++;
            return Leaderboard.TryGetValue(name, out int score) ? 
                Results.Ok(new OutputScore(name, score)) : 
                Results.NotFound(new {Name = name});
        }

        /// <summary>
        /// Returns the Scoreboard sorted by score.
        /// </summary>
        /// <returns>
        /// 200 OK with the scoreboard sorted by score.
        /// </returns>
        private static IResult ReturnScoreboardByScore()
        {
            scoreboardRetrievalsSinceStartup++;
            return Results.Ok(sortedScores);
        }
        
        /// <summary>
        /// Returns the Scoreboard sorted by Name.
        /// </summary>
        /// <returns>
        /// 200 OK with the scoreboard sorted by name.
        /// </returns>
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

            scoreboardRetrievalsSinceStartup++;
            return Results.Ok(sorted);
        }

        /// <summary>
        /// Returns the dictionary as an ordered list.
        /// </summary>
        /// <returns>A List of type ScoreEntry sorted in descending order by score.</returns>
        private static List<ScoreEntry> ReturnOrderedList()
        {
            var snapshot = Leaderboard;
            var sorted = snapshot
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => new ScoreEntry(kvp.Key, kvp.Value))
                .ToList();

            return sorted;
        }
        #endregion
    }
}
