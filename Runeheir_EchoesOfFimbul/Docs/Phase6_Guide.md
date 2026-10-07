# RUNEHEIR — Phase 6 Guide: Multiplayer, Vending & the Alpha

Phase 6 puts Midgard online. One player **hosts a realm** (or a headless **realm server** runs one), and friends **join** it by address. Everyone shares the 14 maps: they see each other walk, fight and cast, and fight the **same monsters and bosses**, which the realm runs. On top of that come the GDD §8 social systems: **chat channels**, **parties with Even Share EXP**, **guilds**, **player trades**, and **street stalls in Vigrid Haven** with the **Merchant Pushcart**.

Offline play is unchanged: **Play Offline** on the new realm screen is the game as it was in Phase 5.

| GDD item | Where it lives |
|---|---|
| Networking (Mirror, KCP over UDP) | `Assets/Mirror` (vendored, MIT); `Scripts/Net/` (the `Runeheir.Net` assembly) |
| Host / Join / dedicated server | `Net/RealmLauncher.cs`; the realm screen `Runtime/FrontEnd/RealmScreen.cs` |
| The realm: accounts, characters, maps, chat, social | `Net/RealmServer.cs`, `RealmServer.Social.cs`; client side `Net/RealmClient.cs`, `NetworkAccountService.cs` |
| Networked players and monsters | `Net/NetPlayer.cs`, `NetMonster.cs`, `NetSpawning.cs` (spawning, per-map visibility) |
| Every map in one shared scene | `Core/World/WorldGrid.cs`, `MapLayout.Translate`; `Runtime/World/WorldBuilder.cs` |
| §8 parties and Even Share (30-level gap) | `Core/Social/Party.cs` |
| Guilds | `Core/Social/Guild.cs` |
| Player trades | `Core/Social/PlayerTrade.cs`, `ItemTransfer.cs` |
| §8 Merchant Pushcart (+8,000 weight) and street vending | `Core/Social/Pushcart.cs`, `Vending.cs` |
| Chat channels | `Core/Social/Chat.cs`; `Runtime/UI/Hud/ChatWindow.cs` |
| Social windows | `Runtime/UI/Hud/SocialHud.cs`, `PartyGuildWindows.cs`, `TradeVendingWindows.cs` |
| Realm settings | `Core/Social/RealmConfig.cs` (`realm.json`) |

---

## 1. Try it in 5 minutes

1. Download the branch again and open the project in Unity 6.3. Run **Runeheir ▸ Setup ▸ Build Prototype Scenes**.
2. Make a player build: **Runeheir ▸ Build ▸ Windows Player** (or Linux Player). It lands in `Builds/Windows/Runeheir.exe`.
3. Start the build. On the realm screen, press **Host** (port `7777`). Register an ID, create a character, **Start**.
4. In the editor, press **Play** in `RH_Login`. On the realm screen type `127.0.0.1` under **Join a Realm** and press **Join**. Register a second ID and make a second character.
5. Both characters stand in Vigrid Haven, and each sees the other. Now try:
   - type `hello` in chat (**Enter**): both windows show it;
   - click the other player: **Trade**, **Invite to party**, **Invite to guild**, **Whisper**;
   - press **Z**, make a party, invite the other player, switch EXP to **Even Share**, then go to the plains together and kill something;
   - talk to **Gunnar** (by the courier, south of the plaza): rent a Pushcart, press **V**, put items up for sale and open your stall. The other player clicks you to buy.

You can also run two builds on one PC, or play on two PCs on the same network (use the host's LAN address, for example `192.168.1.20`).

## 2. The realm screen

After the splash, the game asks how you want to play:

- **Play Offline:** single-player, exactly as before. Accounts and characters stay in this PC's offline save.
- **Host a Realm:** runs a realm inside your game and logs you into it. Friends join with your address. **Their accounts and characters are saved on your PC**, in the realm's data folder (§5).
- **Join a Realm:** connects to a host or a dedicated server. The address can be `host`, `host:port` or `[ipv6]:port`. Without a port it uses `7777`.

From the login screen, **Realms** (or **Escape**) brings you back here. If the connection drops in the middle of a game, you return to the realm screen with a message saying so.

## 3. Hosting for friends

- The realm listens on **UDP** port **7777** by default. You can change the port on the realm screen or in `realm.json`.
- **Same network (LAN):** friends join with your PC's local address (`ipconfig` on Windows, `ip addr` on Linux). Allow the game through the firewall when Windows asks, for **private networks**.
- **Over the internet:** forward **UDP 7777** on your router to your PC, then give friends your public address. Mobile and some home connections (CGNAT) can't take incoming connections; use a dedicated server on a VPS instead (§4).
- **When the host quits, the realm stops** and everyone is sent back to the realm screen. Their characters are saved on the host's PC, so they're there the next time the host opens the realm.

## 4. Dedicated realm server

A realm server is a headless build of the game: no window, no player, just the realm and its connections.

1. In **Unity Hub ▸ Installs ▸ 6000.3.x ▸ Add modules**, add **Linux Dedicated Server Build Support** (and/or **Windows Dedicated Server Build Support**).
2. **Runeheir ▸ Build ▸ Linux Realm Server (headless)** builds `Builds/LinuxServer/RuneheirRealm.x86_64`. The Windows one builds `Builds/WindowsServer/RuneheirRealm.exe`.
3. Copy the folder to the server and run it:

   ```bash
   ./RuneheirRealm.x86_64 -realmData /srv/runeheir -port 7777 -logFile /srv/runeheir/realm.log
   ```

   - `-realmData <folder>`: where the realm keeps its files (§5). The first start writes a default `realm.json` there.
   - `-port <number>`: overrides the port in `realm.json`.
   - Open **UDP** on that port in the server's firewall (for example `sudo ufw allow 7777/udp`).
4. Players **Join** with the server's address.

Any normal build also starts as a realm server with `-server`, for example `Runeheir.exe -batchmode -nographics -server`.

**From the command line** (CI or a build machine):

```bash
Unity -batchmode -quit -projectPath Runeheir_EchoesOfFimbul -buildTarget Linux64 \
  -executeMethod Runeheir.EditorTools.RuneheirBuild.BuildFromCommandLine -realmServer \
  -customBuildPath Builds/LinuxServer/RuneheirRealm.x86_64
```

## 5. Realm settings and files

Every realm has a data folder:
- a hosted realm uses the game's data folder plus `realm`. On Windows that's `%USERPROFILE%\AppData\LocalLow\Runeheir Studio\RUNEHEIR - Echoes of Fimbul\realm`;
- a server uses `-realmData`.

| File | What it holds |
|---|---|
| `realm.json` | The realm's settings (below). Edit it while the realm is stopped. |
| `realm_accounts.json` | Accounts (PBKDF2-hashed passwords), their characters and their storage |
| `realm_guilds.json` | Guilds, members, ranks and notices |

`realm.json`. Every value is checked when it loads, so a mistake can't break the realm:

| Setting | Default | What it does |
|---|---|---|
| `Name` | `Runeheir Alpha` | The realm's name, shown at login |
| `Motd` | a welcome line | Shown to everyone who enters the world |
| `Port` | `7777` | UDP port |
| `MaxPlayers` | `64` | 1–500. Players past this are turned away |
| `AllowGmCommands` | `true` | Lets players use `@` commands. **Turn it off for a public realm.** |
| `AllowRegistration` | `true` | Lets new IDs register. Turn off to keep the realm to existing accounts |
| `BaseExpRate`, `JobExpRate` | `50`, `50` | EXP multipliers (0–1000) |
| `DropRate`, `CardDropRate` | `5`, `1` | Drop multipliers (0–100) |

Back these files up to back up the realm. Characters on a realm are separate from offline characters.

## 6. Chat

Press **Enter**, type, and press **Enter** again.

| Type | Who sees it |
|---|---|
| `text` or `/s text` | Everyone on your map |
| `%text` or `/p text` | Your party |
| `$text` or `/g text` | Your guild |
| `/sh text` (`/y`, `/shout`) | Everyone on the realm. Once every 10 seconds |
| `/w Name text` (`/t`, `/tell`, `/whisper`) | One player, on any map. Names with spaces go in quotes: `/w "Net Tester" hi` |
| `/r text` | Answers the last whisper |
| `@command` | GM and helper commands (when the realm allows them) |

Messages are up to 120 characters. The realm mutes anyone sending more than 6 lines in 4 seconds for a moment. Colours: party pink, guild green, whispers yellow, shouts orange.

## 7. Parties (Z)

- **Make one:** press **Z**, type a name, **Create party**. **Invite** by typing a player's name, or click a player and choose **Invite to party**.
- Up to **12 members**. The window shows each member's level, job, map and HP, and the leader (★).
- The leader can **Make leader** (hand over) and **Expel**. **Leave party** works for anyone. When the leader leaves, the lead passes on.
- **EXP: Each Takes** (default): whoever lands the kill keeps its EXP.
- **EXP: Even Share** (GDD §8): a kill's base and job EXP is split between **every living party member on the same map**, as long as the party's highest and lowest base levels are **at most 30 apart**.
  - The pool grows by **10% for each extra member**. For example, 1,000 EXP shared by 3 gives each 400.
  - The window shows the party's level gap. If a new member or a level-up pushes it past 30, Even Share switches off and the party is told why.
- Party chat: `%text`.

## 8. Guilds (G)

- **Found one** from **base level 30** for **50,000 zeny**: press **G**, type its name (3–24 characters), **Found guild**.
- Up to **30 members**. Ranks: **Leader** (♛), **Officer** (◆) and **Member**. The leader and officers can invite, expel members and set the **notice**. Only the leader can promote or demote, hand over the lead, or **disband**.
- The guild's name shows under its members' names. Guild chat: `$text`.
- Guilds are saved on the realm (`realm_guilds.json`) whenever they change.

## 9. Trading

1. Click a player standing within **6 m** and choose **Trade**. They get a prompt to accept.
2. Both sides put up to **10 items** and some **zeny** on the table.
3. Press **OK (lock)** to freeze your side. When both sides are locked, press **Trade**. Either side can **Cancel** until both have pressed Trade.
4. The realm checks that each side still has its goods and can carry the other side's, then swaps them. If anything doesn't fit, the trade is cancelled and nothing moves.

## 10. The Pushcart and street stalls (V)

- **Gunnar** (Pushcart Rental, south of the plaza in Vigrid Haven) rents the **Merchant Pushcart** for **1,500 zeny** from **base level 10**. It's yours to keep: **+8,000 weight capacity** (GDD §8), and you can vend.
- **Open a stall:** press **V** in Vigrid Haven. Name the stall, pick up to **12 items** from your bag, set a quantity and a price each, then **Open stall**.
  - The goods move into your cart hold while the stall is open. Other players see a **[Shop]** sign with the stall's name over you.
  - You mind your stall: you can't walk until you close it, and leaving the spot (a warp, a skill that moves you) closes it.
  - Stalls keep a little room around them: stand away from NPCs, portals and other stalls.
- **Buy:** click a player with a stall sign. Their stall opens: pick an item, a quantity, and **Buy**. You pay first, and the realm hands the goods over. If something goes wrong, you get your zeny back.
- **Sell:** zeny from each sale goes straight to you, with a chat line saying who bought what.
- **Close** the stall with **V** again: unsold goods return to your bag. If the game closes while a stall is open, the goods wait in the cart hold. Gunnar's **Unload my Pushcart hold** puts them back in your bag.

## 11. How the shared world works

- **One scene, every map.** On the realm, each map is built at its own spot: a 4-column grid, 1,000 m apart. Players only see what's on their own map (per-map interest management), so the maps don't mix. Saved positions stay relative to their map, so old saves are unaffected.
- **Who runs what:**
  - The **realm** runs monsters, bosses, boss timers, drops, chat and every social system.
  - **Each client runs its own character:** movement, stats, skills and inventory. It saves the character to the realm.
  - Other players are mirrored on your screen: position, gear look, attacks, casts, statuses and HP.
- **Fighting together:** hits, statuses, knockbacks, heals and buffs between players and monsters are relayed to whoever runs the target. Monster rewards (EXP, loot, MVP prizes) go from the realm to the player who earned them, through Even Share when it's on.
- **Changing maps:** your game saves the character to the realm, loads the new map, and the realm places you there.

## 12. Alpha limitations

This is an **alpha**. Before you open a realm to strangers:

- **Characters are run by their own clients.** A modified client could cheat its own character (stats, items, zeny). Play with people you trust; a server-run character is planned for after the alpha.
- **Nothing is encrypted.** Logins travel over plain UDP. **Use a password you don't use anywhere else.**
- **Boss timers reset when the realm restarts.** They're kept for as long as the realm runs.
- **GM commands are on by default** (`AllowGmCommands`). On someone else's realm, the commands that change the world (`@monster`, `@spawn`, `@bossrespawn`, `@bosstime`) are host-only.
- **No PvP yet.** Players can't hurt each other.
- **If you disconnect, log in again.** The game doesn't resume a dropped session on its own.
- Placeholder art and sound as in Phase 5.

## 13. Save data

- **New on each character:**
  - `HasPushcart`;
  - `Cart`: the cart hold, saved with the character so stall goods are never lost.
- Offline saves load unchanged. Offline and online characters are separate: an offline character can't be brought onto a realm.

## 14. Tests

- **Core (no Unity needed):** `cd Tools/CoreTests && dotnet test Tests` runs **166 tests** (20 new):
  - party rules (invites, leaders, the 30-level Even Share gap and the shared EXP);
  - trades (lock, confirm, exact item matching, weight and bag space);
  - the Pushcart's weight and rental;
  - stall checks (titles, prices, quantities, taking and handing over goods);
  - guild ranks, founding, saving and rollback;
  - chat parsing, cleaning and the flood gate;
  - realm settings and addresses;
  - map placement on the grid and the buff/status codec used on the wire.
- **PlayMode (`NetworkSmokeTests`):** hosts a realm inside the test, then:
  1. registers, logs in and creates a character over the network;
  2. enters Vigrid Haven through the realm and checks the character is networked;
  3. kills a realm-run monster for EXP;
  4. echoes a chat line through the realm;
  5. makes a party with Even Share;
  6. opens and closes a street stall;
  7. shuts the realm down.
- The code compiles against Unity 6.3's own DLLs, and Mirror's weaver processes `Runeheir.Net` with no errors.

## 15. Design choices to confirm

1. **Hybrid authority for the alpha:** each player's game runs its own character. This is simpler and smooth to play, but trusts clients (§12).
2. **Host = server:** a hosted realm keeps its friends' characters on the host's PC. A dedicated server is the way to keep a realm up all the time.
3. **Even Share** needs members on the **same map** and alive, as in Ragnarok.
4. **Stalls only in Vigrid Haven**, and the Pushcart is open to every class, from base level 10.
5. **The guild founding fee** is 50,000 zeny, from base level 30.

## What's next

**The full class roster:** every Ragnarok/XileRO job with a Norse name:
- the first jobs, second jobs and transcendent jobs, plus the special classes;
- their skills;
- **rebirth** at Urðr's Well. You become a High Initiate and can only follow your first life's path, for example Warrior → Berserker → reborn → High Warrior → Einherjar.
