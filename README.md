# Join Us!
For more mods, news, and support join us on discord here: https://discord.gg/mGfsTvc53v

# UnboundLib
This is a helpful utility for ROUNDS modders aimed at simplifying certain common tasks.

# Usage

## NetworkingManager
The **NetworkingManager** abstracts the default Photon networking capabilities away into an easy-to-use interface you can use for communication between clients.
Example usage:
```c#
	private const string MessageEvent = "YourMod_MessageEvent";

	NetworkingManager.RegisterEvent(MessageEvent, (data) =>
	{
		ModLoader.BuildInfoPopup("Test Event Message: " + (string)data[0]);    // should print "Test Event Message: Hello World!"
	});

	// send event to other clients only
	NetworkingManager.RaiseEventOthers(MessageEvent, "Hello World!");
	
	// send event to other clients AND yourself
	NetworkingManager.RaiseEvent(MessageEvent, "Hello World!");
```

## CustomCard Framework
Create a class for your card, extend the **CustomCard** class, implement its methods, and then in your mod initialization register your card like so:
```c#
void Start()
{
    CustomCard.BuildCard<TestCard>();
}
```

## CustomMap Framework
First create a map in unity by using the package and export you're scene to a AssetBundle. Then in your mod initialization register your map like so:

```c#
void Start()
{
    Unbound.BuildLevel(TestAssetBundle);
}
```

## GameMode Framework
The GameMode framework provides a modding-friendly API for custom game modes, allowing mods to target them without having to know about their existence.

Custom game modes are defined in two layers: the actual game mode class that does all the heavy gameplay logic, and a handler class that provides an interface
between mods and the game mode.

### Hooks
The framework offers a flexible hook system. With hooks, mods can trigger actions at specific points of time as a game is running, without needing to know
anything about the specific game mode or its implementation.

#### Triggering hooks
Game modes can trigger async hooks whenever they wish:

```csharp
private IEnumerator RoundStart()
{
	// Hook keys are case-insensitive
	yield return GameModeManager.TriggerHook("RoundStart");

	// A healthy set of predefined keys is provided to make hooking on to them easier.
	// Predefined keys should be used in favour of custom ones when possible.
	yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
}
```

#### Registering hooks
Mods can register hook listeners wherever they wish:

```csharp
private void Init()
{
	// Hooks are called with the game mode that triggered the hook, which is always the currently active game mode
	GameModeManager.AddHook(GameModeHooks.HookRoundStart, this.OnRoundStart);
}

private IEnumerator OnRoundStart(IGameModeHandler gm)
{
	// Triggers are IEnumerators so they support yields
	yield return new WaitForSeconds(2f);

	UnityEngine.Debug.Log(gm.Name);

	/* Since triggers are IEnumerators, they must be executed within a coroutine. This means triggers are guaranteed to
	 * be able to disrupt the execution of the current game mode.
	 */
	gm.GameMode.StopAllCoroutines();
}
```

The existing game modes in ROUNDS, namely Arms Race and Sandbox, have also been patched to trigger hooks.

### Settings
The framework also adds a setting system to help mods change common game mode settings easily. Settings provide an easy-to-use method for mods to change gameplay,
but they place a lot of responsibility onto game modes to provide sufficient settings.

#### Using settings in a game mode

```csharp
private void CheckPoints()
{
	if (p1Points >= (int)GameModeManager.CurrentHandler.Settings["pointsToWinRound"])
	{
		WinRound();
	}
}
```

#### Changing settings (in a mod)

```csharp
private void Init()
{
	GameModeManager.AddHook(GameModeHooks.HookInitEnd, this.OnInitEnd);
}

private IEnumerator OnInitEnd(GameModeHandler gm)
{
	gm.ChangeSetting("pointsToWinRound", 10);
}
```

---

See [/GameModes](./GameModes) for implementation details and example `GameModeHandler`s.

# Development

Building the project is likely to work without having to change anything. However, if your ROUNDS installation resides in somewhere other than `C:\Program Files (x86)\Steam\steamapps\common\ROUNDS`,
you will need to change the path we have pre-configured for you:

1. Copy `Source/UnboundLib.csproj.user.dist` to `Source/UnboundLib.csproj.user`
2. Change the ROUNDS installation folder path inside `Source/UnboundLib.csproj.user`

You can now open the project solution with Visual Studio, and you're set.