# Simple Scoreboard API
This repository contains a C# file that is a simple API that you can push scores to.
You can also retrieve the scoreboard by numerical order, or by alphabetical order.

This API runs entirely on Memory. Data will be lost when it's restarted or when it's quit.
I may add persistence later, but for now it's outside of the scope of this project.

The default settings for the .csproj run the API on your local network's port 5228. 
For example, if your computer's IPv4 Address is 192.168.1.2, you can call `http:192.168.1.2:5228/swagger` in any browser connected to your router to open the swagger interface.

The following are behaviors of the API:

- GET `/` returns code 200 and basic health information.
- GET `/uptime/` returns code 200 and the current uptime of the API in a JSON friendly format. Displayed as `{Days,Hours,Minutes,Seconds}`.
- GET `/leaderboards/{name:<name>}` returns the score that is registered with that name, if it exists. These are the following return Codes:
  - 404 Error if the name does not exist
  - 200 if the name does exist. Will also return the JSON data formatted as: {"name":string,"score":int}
- GET `/leaderboards/sortByName` returns code 200 and a list sorted by LINQ. This list is sorted in alphabetical order by name.
- GET `/leaderboards/sortBySore` returns code 200 and a list sorted by LINQ. This list is sorted in descending order by score.
- POST `/leaderboards/{name:<name>, score:<score>}` adds a new score to the scoreboard. These are the following return Codes:
  - 400 Bad Request if the name is longer than 64 characters.
  - 201 Created If a new score was created.
  - 200 OK If the name already existed and the score was updated.

Developer Mode Behaviors:
- GET `/debugStats` returns code 200 along with developer debug stats like start time of the API, UpTime, and the number of Get/Post requests since start.
- GET `/leaderboard/dump` returns code 200 and returns the unsorted dictionary for the scoreboard. Only available in developer mode.
