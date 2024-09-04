<< Bug (IL2CPP TCP can never re-connect after disconnect) >>
* Make a build of the project with IL2CPP
* Repeat the connection steps in "Bug 1" so server/client are connected
* Now have the client disconnect
* Now have the client try to connect again (he will never be able to)
* If you do this under the Mono runtime in Unity, it works perfect