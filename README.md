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

# GameMode Framework
The GameMode framework provides a modding-friendly API for custom game modes, allowing mods to target them without having to know about their existence.

Custom game modes are defined in two layers: the actual game mode class that does all the heavy gameplay logic, and a handler class that provides an interface
between mods and the game mode.

## Hooks
The framework offers a flexible hook system. With hooks, mods can trigger actions at specific points of time as a game is running, without needing to know
anything about the specific game mode or its implementation.

### Triggering hooks
Game modes can trigger hooks whenever they wish:

```csharp
private void RoundStart() {
  // Hook keys are case-insensitive
  GameModeManager.TriggerHook("RoundStart");

  // A healthy set of predefined keys is provided to make hooking on to them easier.
  // Predefined keys should be used in favour of custom ones when possible.
  GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
}
```

### Registering hooks
and mods can register hook listeners wherever they wish:

```csharp
private void Init() {
  // Hooks are called with the game mode that triggered the hook, which is always the currently active game mode
  GameModeManager.AddHook(GameModeHooks.HookRoundStart, (gm) => UnityEngine.Debug.Log(gm.Name));
}
```

The existing game modes in ROUNDS, namely Arms Race and Sandbox, have also been patched to trigger hooks.

## Settings
The framework also adds a setting system to help mods change common game mode settings easily. Settings provide an easy-to-use method for mods to change gameplay,
but they place a lot of responsibility onto game modes to provide sufficient settings.

### Using settings in a game mode

```csharp
private void CheckPoints() {
	if (p1Points >= (int)GameModeManager.CurrentHandler.Settings["pointsToWinRound"]) {
		WinRound();
	}
}
```

### Changing settings (in a mod)

```csharp
private void Init() {
	GameModeManager.AddHook(GameModeHooks.HookInitEnd, (gm) =>
	{
		gm.ChangeSetting("pointsToWinRound", 10);
	});
}
```

---

See [/GameModes](./GameModes) for implementation details and example `GameModeHandler`s.
