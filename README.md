# UnboundLib
This is a helpful utility for ROUNDS modders aimed at simplifying certain common tasks.

# NetworkingManager
The **NetworkingManager** abstracts the default Photon networking capabilities away into an easy-to-use interface you can use for communication between clients.

## Events

Example usage:
	
	private const string MessageEvent = "YourMod_MessageEvent";

	NetworkingManager.RegisterEvent(MessageEvent, (data) => {
	  ModLoader.BuildInfoPopup("Test Event Message: " + (string)data[0]);    // should print "Test Event Message: Hello World!"
	});

	// send event to other clients only
	NetworkingManager.RaiseEventOthers(MessageEvent, "Hello World!");
	
	// send event to other clients AND yourself
	NetworkingManager.RaiseEvent(MessageEvent, "Hello World!");

## RPC

**NetworkingManager** adds RPC support for static methods. With RPCs you can call methods on remote clients in the same room. Photon's PUN package offers similar functionality, but only for instance methods. Photon's RPC functionality is also difficult to setup from scripts.

To register a function as an RPC, add an **UnboundRPC** attribute to your method, and provide it with some unique ID.

	[UnboundRPC("MyMod.SomeMethod")]
	public static void SomeMethod(string message) {
		
	}

You also need to tell **UnboundLib** to register all your RPCs.

	// Somewhere in your mod's initialization code
	NetworkingManager.RegisterRPCHandlers();

To call an RPC on all connected clients (including yours), use the **NetworkingManager.RPC** method

	// The first argument is the ID you gave to your RPC method. Subsequent arguments will be forwarded to the RPC method.
	NetworkingManager.RPC("MyMod.SomeMethod", "Hello there");

# CustomCard Framework
Create a class for your card, extend the **CustomCard** class, implement its methods, and then in your mod initialization register your card like so:

  	void Start()
  	{
	  CustomCard.BuildCard<TestCard>();
  	}
