NOTE: To be very clear, these bugs ONLY happens under Unity3D runtimes or libraries NOT .NET 8 or Mono running nativly on Windows or Linux.

<< Bug 1 (new data gets old data) >>
* Open "BugTestScene" in two editors on two different computers
* Follow the prompt as if it was a console app
	- Use TCP should be 'y'
	- Your IP address should be the one you get with "ipconfig or ifconfig" in a terminal
	- The server IP address is the remote address a client wants to connect to
* Now send very long messages from the server PC so its sending a lot of data
* Here is some message data you can copy and paste to repo issue: "11111111111111111111111111111111111111111111111111111111111111111111112222222222222222222222222222222222222222222222222222222222222222233333333333333333333333333333333333333333333333333333333333333333334444444444444444444444444444444444444444444444444444444444444445555555555555555555555555555555555555555555555555555555555555555556666666666666666666666666666666666666666666666666666666666666777777777777777777777777777777777777777777777777777777777777777888888888888888888888888888888888888888888888888888888888888888888889999999999999999999999999999999999999999999999999999999999"
* When you send a message, it will send it 5 times changing the first char to a letter to make each packet unique and to test a bunch of messages sent all at once (aka spam the socket API with multiple send commands)
* You SHOULD now see the message you send on the clients Console log 5 times with the first two chars changed to something like "a_", "b_", "c_" etc
* HOWEVER, you see the message with multiples of "b_" or "c_"

<< Bug 2 (IL2CPP TCP can never re-connect after disconnect) >>
* Make a build of the project with IL2CPP
* Repeat the connection steps in "Bug 1" so server/client are connected
* Now have the client disconnect
* Now have the client try to connect again (he will never be able to)
* If you do this under the Mono runtime in Unity, it works perfect