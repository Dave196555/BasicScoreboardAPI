# Simple Scoreboard API
This repository contains a simple C# file which is a simple API that you can push scores to. 

The default settings for the .csproj run the API on your local network's port 5228. 
For example, if your computer's IPv4 Address is 192.168.1.2, you can call http:192.168.1.2:5228/swagger in any browser to open the swagger interface.

The following are behaviors of the API when called from a web browser:

- IP/ simply returns a Hello World message.
- IP/Time/ returns the current time according to the clock of the computer. (Uses DateTime.Now)
- IP/leaderboards/{name:<name>} returns the score that is registered with that name.

You can also push a score to the API using /leaderboards/{name:<name>, score:<score>}.
