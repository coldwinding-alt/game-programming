# Migration Status

Updated: 2026-04-18

## Current State

The project is now a Unity 2022.3 playable baseline derived from the original H5 source and assets in `_reference_do_not_ship/basketball-legends-2020-ovo`.

Unity currently supports:

- menu boot flow
- quick match / training entry
- arena, basket, ball, player, and HUD runtime objects
- original atlas frame key loading
- original DragonBones data subset loading for current gameplay player usage
- Windows build output that can be launched locally

## Latest Improvements

This iteration focused on the biggest visible fidelity gaps from live play feedback:

- UI text scale fixed across menu and match HUD
- outline/shadow text rendering added so labels read closer to the original H5 presentation
- menu controls text cleaned up and resized for the actual page scale
- scoreboard styling improved with stronger score digits and timer placement closer to the original
- pre-match countdown added
- timer formatting changed toward the original match display style
- keyboard double-tap dash timing moved closer to original thresholds
- player-to-player steal interaction added
- AI now reacts to the live ball holder instead of only loose-ball situations

Follow-up cleanup after immediate playtest feedback:

- reduced the heavy fake-outline text layering that was causing dirty black artifacts behind menu and HUD text
- tightened countdown/message text cleanup so `GO!!!` does not linger after the pre-match intro
- changed countdown start timing so it cleanly begins from `3`

Latest gameplay-fidelity pass:

- replaced the simplified direct handoff steal with an original-style steal lane check
- steal attempts now resolve after a short action window instead of immediate overlap contact
- stolen players now enter `stun` and the ball is knocked free using the original steal velocity constants
- AI defenders now position into a steal lane and use cooldown-based steal attempts instead of stealing instantly on overlap
- AI ball carriers now react better to close defenders so straight-line drives can beat the first reach more naturally
- end-of-match flow now shows `TIME!!!`, restarts overtime after a delay, and uses a smaller post-match overlay instead of leaving an oversized winner label on the court

Latest accessibility / editor pass:

- added two AI difficulty levels exposed from the main menu: `AI: EASY` and `AI: NORMAL`
- easy AI does not attempt steals against a live ball handler; it keeps defensive spacing and only jumps to contest near the basket
- kept the project in a standard Unity layout with `Assets`, `Packages`, `ProjectSettings`, and enabled `Assets/Scenes/Main.unity`
- verified editor-side compile again through Unity batchmode
- later batch build/smoke runs were blocked because the same project was already open in another Unity editor instance, which also confirms the project opens in-editor as intended

Latest stun-fidelity pass:

- verified from original H5 gameplay code that grounded opponents can be stunned by a steal even when they are not holding the ball
- verified from original DragonBones gameplay data that `stun` runs for 22 frames at 30 fps, about `0.73s`
- Unity steal target checks now allow empty-hand stun in the original forward steal lane
- Unity stun lock is now separated from the shorter steal action timing, so the dazed state lasts closer to the original game
- `STEAL!` center text now only appears when a steal actually knocks the ball free

Latest countdown / pickup fidelity pass:

- verified from original H5 HUD flow that the `GO!!!` countdown info object is explicitly hidden after the final tween step
- added explicit `HideMessage` / `HideCountdown` cleanup plus a fallback clear path so stale `GO!!!` text does not survive scene flow transitions
- verified from original H5 collision code that loose-ball pickup is driven by the player hand sensor (`cbPlayersHands`) and `canTakeInHands`, not by pressing the steal button
- matched that sensor more closely in Unity with a contact-style pickup box based on the original `30 x 80` hand sensor plus ball radius
- moved loose-ball pickup to a per-frame closest-player contact check while preserving steal/stun windows that temporarily disable pickup

Latest hoop / scoring fidelity pass:

- verified from original H5 basket code that the hoop uses a glass body, two colliding rim circles, a non-colliding center ring body, and separate upper/down score sensors
- verified from original H5 `MatchProcessor` that scoring is gated by `upper sensor -> down sensor`, and that hitting the down sensor first cancels the score
- replaced the Unity one-step near-hoop score guess with upper/down sensor tracking that is armed on shot release, so visible swishes and actual points stay closer together
- replaced the coarse rim proximity bounce with substepped rim-circle collision against the left/right rim contact points
- replaced the generic hoop-side bounce with a backboard collision that uses the original glass placement and dimensions

## Validation

- Unity compile pass completed after the latest UI/gameplay changes
- Dedicated Windows build entry point added:
  - `Assets/BasketballLegends2020/Scripts/Editor/BL2020BuildTools.cs`
- Windows build log confirms:
  - `BL2020 Windows build passed: Builds/Windows/BasketballLegends2020.exe`
- A new batch compile attempt for the latest hoop/scoring pass was blocked because the same project was already open in another Unity editor instance:
  - `Logs/unity-compile-hoop-fidelity.log`

## Still Missing For Higher Fidelity

- fuller `PlayerObject` behavior parity:
  - block / pump fake flow
  - dunk / alley-oop logic
  - better landing / throw timing
  - super-shot behavior
- closer physics equivalence to original Nape interactions
- fuller AI delay tables and strategy migration
- more complete menu flow parity:
  - team select
  - player select
  - pause / help / post-match screens
  - tournament flow
- broader DragonBones runtime coverage:
  - event frames
  - richer animation transitions
  - draw-order and color behavior parity

## Near-Term Focus

1. Continue HUD/menu fidelity using direct original layout/style references.
2. Keep tightening on-court interactions that most affect hand feel, especially real block / pump / dunk behavior.
3. Port the next batch of original player and AI state logic.
4. Keep every meaningful step buildable and runnable in Unity 2022.
