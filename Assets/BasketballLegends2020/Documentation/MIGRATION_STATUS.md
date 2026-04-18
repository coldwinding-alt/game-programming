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

Latest reference-parity pass:

- added `REFERENCE_PARITY_MAP.md` to track original H5 modules against the Unity migration modules
- re-checked original `PlayerObject.makeThrow`, `makeDunk`, `endDunk`, and `BallObject.dunk`
- added airborne dunk zone checks using the original `DUNK_ZONE1_Y`, `DUNK_ZONE2_Y`, `DUNK_X`, and `DUNK_Y` constants
- added the original three dunk timing paths and ball-release values for completed and missed dunks
- updated AI attack behavior so a ball carrier can jump near the dunk lane and trigger the new dunk release path instead of only taking simplified shots

Latest block / pump / AI strategy fidelity pass:

- re-checked original `PlayerObject.makeBlockOrPump`, `setBlock`, `releaseBlockOrPump`, `unBlock`, and animation-complete handling
- added Unity-side block and pump phases that follow the original start, hold, and end timing from `sk2.json`
- holding the action key now becomes pump fake when the player has the ball and block when the player does not have the ball
- block now disables loose-ball hand pickup while active, then restores pickup on release, matching the original `canTakeInHands` flow
- added a block collision window using the original front-of-block direction check from `onBallBlock`
- blocked shots now enter the ball `block` state and are knocked away from the blocker instead of passing through the player body
- reworked normal AI defence toward the original `strategyDefence`: keep spacing from the ball holder, react to pump fake, jump to contest, block in range, and avoid instant overlap steals
- reworked AI attack toward the original `strategyAttack`: choose an attack point, derive a jump point, drive into the lane, pump against close defenders, and decide dunk/shot in the air

Latest dunk scoring / dash input fidelity pass:

- fixed an intermittent no-score case where a dunk looked made on screen but points were not awarded
- added a short post-dunk pickup lock window so the ball cannot be picked up before score sensors resolve
- for completed dunks, sensor arming now follows a reference-aligned path to avoid false down-first misses caused by coarse substep timing
- tightened dash double-tap detection to match the original keyboard pattern more closely:
  - detect dash by `key up -> next key down` within the original-style `460 ms` window
  - removed the stricter down-to-down-only check that could miss first-attempt dashes
- added a short dash input buffer so valid double-tap input is not dropped between animation/state transitions

Latest source-equivalent core pass:

- added dedicated delay types to mirror original timing semantics:
  - `FullDelay`
  - `UseDelay`
  - `AIUseDelay`
  - `SimpleDelay`
  - `NegativeDelay`
- added a gameplay signal bus so AI strategy switching can follow player events instead of implicit polling:
  - `startSteal`
  - `steal`
  - `jumpA`
  - `pump`
  - `dash`
  - `stun`
- added a `MatchProcessor`-style score context chain for shot/block/sensor ordering parity
- moved player action resolution toward frame-event driven timing:
  - `throw_land -> throw`
  - `steal -> action`
  - `dunk1/2/3 -> dunk`
  - with animation-complete fallback handling
- restructured AI to `Base + AIController + AIController2` with explicit strategy functions:
  - `strategyDefence`
  - `strategyDefence2`
  - `strategyBallFight`
  - `strategyAttack`
  - `strategyJumpBall`
  - `strategyRebound`
- added controller lifecycle callbacks from player state machine:
  - `playerOnGround`
  - `playerOnDashEnd`
  - `playerOnBlock`
- applied first-dash reliability fix by advancing dash-delay warmup during pre-match countdown
- corrected a defence timing bug where steal delay was sampled twice in one frame and could drop action windows
- tightened score consistency by making basket down-sensor settlement require both:
  - local ball sensor order check
  - `MatchProcessor.ProcessSensor` approval
- added `REFERENCE_FUNCTION_PARITY_MATRIX.md` as the primary function-level parity checklist
- added `PARITY_SIDE_BY_SIDE_CHECKLIST.md` as the side-by-side acceptance checklist for runtime behavior/timing/trigger validation

Latest match-core parity pass (round 2):

- added `BLAISkillsData` as a direct skill-profile mapping layer for reference-aligned AI/player parameters
- player throw accuracy and dunk completion chance now come from the mapped skill profile instead of fixed constants
- AI controller hierarchy now consumes skill-profile delays/chances across defence/attack reactions
- completed dunk scoring now has a reference-safe fallback path to prevent intermittent “visual dunk made but no score” results when sensor checks are skipped by coarse substeps
- increased dunk-state physics substeps to reduce sensor tunneling in high-speed rim contact moments

## Validation

- Unity compile pass completed after the latest UI/gameplay changes
- Dedicated Windows build entry point added:
  - `Assets/BasketballLegends2020/Scripts/Editor/BL2020BuildTools.cs`
- Windows build log confirms:
  - `BL2020 Windows build passed: Builds/Windows/BasketballLegends2020.exe`
- Latest block / pump / AI strategy pass validation:
  - Windows build succeeded: `Logs/unity-build-block-pump-ai.log`
  - smoke test succeeded: `Logs/unity-smoke-block-pump-ai.log`
- Latest dunk scoring / dash input pass validation:
  - smoke test succeeded: `Logs/unity-smoke-dunk-dash-fix.log`
  - batch Windows build attempt was blocked because the same project was already open in another Unity editor instance:
    - `Logs/unity-build-dunk-dash-fix.log`
- Latest source-equivalent core pass validation:
  - batch smoke/build attempts from CLI are currently blocked if the Unity editor is already open on this project (single-project lock)
  - code-level validation completed through reference cross-check and function parity matrix updates
- Latest match-core parity pass (round 2) validation:
  - smoke passed: `Logs/smoke_round2.log`
  - batch build currently blocked when editor holds project lock:
    - `Logs/build_windows_round2.log`

## Still Missing For Higher Fidelity

- fuller `PlayerObject` behavior parity:
  - exact block / pump fake DragonBones event callbacks
  - alley-oop logic
  - richer dunk animation event timing
  - better landing / throw timing
  - super-shot behavior
- closer physics equivalence to original Nape interactions
- fuller AI delay tables and remaining strategy migration
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
