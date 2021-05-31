# Join Us!
For more mods, news, and support join us on discord here: https://discord.gg/mGfsTvc53v

# UnboundLib
This is a helpful utility for ROUNDS modders aimed at simplifying certain common tasks.

# NetworkingManager
The **NetworkingManager** abstracts the default Photon networking capabilities away into an easy-to-use interface you can use for communication between clients.
Example usage:
	
	private const string MessageEvent = "YourMod_MessageEvent";

	NetworkingManager.RegisterEvent(MessageEvent, (data) => {
	  ModLoader.BuildInfoPopup("Test Event Message: " + (string)data[0]);    // should print "Test Event Message: Hello World!"
	});

	// send event to other clients only
	NetworkingManager.RaiseEventOthers(MessageEvent, "Hello World!");
	
	// send event to other clients AND yourself
	NetworkingManager.RaiseEvent(MessageEvent, "Hello World!");

# CustomCard Framework
Create a class for your card, extend the **CustomCard** class, implement its methods, and then in your mod initialization register your card like so:

  	void Start()
  	{
	  CustomCard.BuildCard<TestCard>();
  	}
