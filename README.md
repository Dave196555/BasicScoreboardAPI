# Simple Scoreboard API
This repository contains a C# file that is a simple API that you can push scores to.
You can also retrieve the scoreboard by numerical order, or by alphabetical order.

The default settings for the .csproj run the API on your local network's port 5228. 
For example, if your computer's IPv4 Address is 192.168.1.2, you can call `http:192.168.1.2:5228/swagger` in any browser to open the swagger interface.

The following are behaviors of the API:

- GET `IP/` simply returns a Hello World message.
- GET `IP/time/` returns the current time according to the clock of the computer. (Uses DateTime.Now)
- GET `IP/leaderboards/{name:<name>}` returns the score that is registered with that name.
- GET `IP/leaderboards/sortByName` returns a list sorted by LINQ. This list is sorted in alphabetical order by name.
- GET `IP/leaderboards/sortBySore` returns a list sorted by LINQ. This list is sorted in descending order by score.
- POST `/leaderboards/{name:<name>, score:<score>}` adds a new score to the scoreboard.
