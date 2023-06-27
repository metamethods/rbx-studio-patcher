# Roblox Studio Patcher
A simple patcher for Roblox Studio that allows you to use the internal state of Roblox Studio.

## How to use
1. Download the latest release from the [releases](https://github.com/metamethods/rbx-studio-patcher/releases) page. 
2. Open your terminal and run `./Roblox Studio Patcher patch` to patch Roblox Studio.
3. Finally watch your dumb little Roblox Studio turn into the cool kid.

## How to unpatch
The orignal file for roblox studio still exists in the same directory as the patched version. To unpatch,
simply just delete the file. *(But you really dont need to lol)*

## Q&A
**Q:** I got the message `You must run this program as administrator to patch roblox studio.` What do I do?
**A:** You just open a new terminal as administrator and run the command again.

**Q:** I got the message `Failed to get roblox studio version.` What do I do?
**A:** You probably dont have a wifi connection, or the url for getting the current roblox studio version isn't up anymore, or just it didn't feel like working. Either way try going to [Roblox Studio AWS S3 Server](http://s3.amazonaws.com/setup.roblox.com/versionQTStudio) and run the commmand with the argument of `--version version-blah`. For example `./EXECUTABLE-NAME patch --version version-d9128aa1071a43de`.