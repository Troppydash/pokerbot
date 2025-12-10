# Poker Bot

## Build and Run
1. Download Jetbrains rider
2. Click run
3. Profit?

## Structure
The core components are
- `Program.cs` is the entry point used to debug, try not to commit it
- `Game.cs` contains the Texas hold 'em no limit implementation, key classes are
  - `Card` for a poker card
  - `Action` for a player action during play
  - `HandResolver` to compare final hands
  - `Game` for the poker game
  - `Game.State` for a complete, one-sided description of the game
- `Agent.cs` provides the interface of a poker playing bot
- `Arena.cs` provides the implementation of a 1v1 poker game tester using `IAgent`s

The `Bots` folder contains bot specific code, the `Bots/Shared` folder contains common components
- `AbstractGame.cs` for a pot proportion variant of poker with fewer abstract actions
- `FastResolver.cs` for a faster version of hand comparisons
- `FileCache.cs` for disk memo computations
- `Helper.cs` for misc deck sampling algorithms methods

## Bots
- `RandomAgent.cs` exports random naive bots
- `EVAgent.cs` for a no-tree-search, equity heuristic bot
- `Cfr` for an attempted cfr poker player (not working)
- `ExpectedMinimax` for the Expectiminimax implementation


### H2H Stats

EVAgent against ExpectedMinimax_Abstract (100 rounds, 200 games, BB = 20, stake = 4000)
- (Round 99) `Player 0 E[Profit] = -4.84BB, 95%-CI = [ -14.23BB, 4.55BB ]`

ExpectedMinimax_Total vs ExpectedMinimax_Abstract
