<< Bug (IL2CPP TCP can never re-connect after disconnect) >>
* Make a build of the project with IL2CPP
* Make it so server/client are connected and can send messages
* Now have the client disconnect
* Now have the client try to connect again (he will never be able to)
* If you do this under the Mono runtime in Unity, it works perfect
* NOTE: same test with a .NET 8 or native Mono app works perfect as well (so the issue is IL2CPP)