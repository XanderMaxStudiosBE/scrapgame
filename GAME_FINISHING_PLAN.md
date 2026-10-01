# SCRAPSHIFT finishing milestones

The goal is a cohesive cozy retro scrapyard game. This document is an implementation sequence, not a release announcement or a claim that the prototype is finished. Unity 6000.3.25f1 / URP 17.3.0, existing saves, controls and authored assets remain the baseline.

## 1. Establish the lighting and workshop look

Source implemented: editable afternoon lighting profile, gradient sky, restrained sky reflections, neutral warm daylight, three focused task lights, contact grounding and explicit preset shadow cascades. Source/data checks pass; real Unity shader compilation, visuals and FPS need verification.

Accept this milestone after the workshop, restoration bench and salvage rows stay readable in Laptop and Balanced; lights have visible fixtures, signs are legible, shadows are stable, materials keep their colors, and stopping Play restores the editor environment. Capture representative Editor and standalone CPU/GPU frame times on the user's laptop before changing budgets.

## 2. Make a full scrapyard day worth playing

Source implemented: one repairable portable radio, three inspectable faults for fans and radios, and explicit repair-versus-salvage costs/work/outcomes. Electronics salvage makes the eastern district useful. Faults persist with items and partial jobs; legacy fan fields/defaults remain compatible. Independent neighbourhood fan/radio requests now reward explicit tests, keep agreed payouts and separate partial deliveries across days/saves. A daily money/work ledger and saved last-day receipt make costs and accomplishments clear; legacy saves label unknown earlier detail until the next full day. Copper orders remain compatible. Keep bounded recipes, exactly-once payments/materials and additive old-save compatibility. Keep the relaxed day loop and avoid fees/deadlines that punish experimentation.

Accept after a new player can find scrap, make a worthwhile repair choice, finish a contract, buy a useful improvement and understand what to try next. Verify partial jobs, rejected inputs, full hands, save/restart and all menu transitions.

## 3. Bring presentation to the same standard

Planned: consistent workshop/office/salvage dressing, readable material variation, purposeful signs, better handling/work animations and mixed audio. Finish one area before applying its art language across the yard. Use original assets; Retro Rewind remains a style reference, not a source of copied content.

Accept after the creator approves actual Unity images and gameplay, including small-window UI, tutorial prompts, all rebound keys/mouse buttons and quiet/cozy audio.

## 4. Finish the player-facing flow

Source implemented: title with Start/Continue/New yard/Settings/Help/Credits/Quit, first-yard introduction, dynamic control help and a six-goal persistent yard journal, Return to title and save-gated Quit. Actual Unity UI/pause/serialization validation and a clean desktop player build remain planned. Preserve settings separately from yard saves and archive resets. Distinguish shipping features from scenery; office/vehicles/crane remain scenery; electronics-salvage radios are now repairable while the shelf radio stays decorative.

Accept after a player can install/open the build, start or continue, complete both processing/repair loops, save/quit/reopen and navigate every menu without editor tools.

## 5. Validate the complete game

Planned: real Unity test runs, player smoke tests, save migration/recovery and representative laptop performance profiling. Resolve reproducible bugs and art/readability feedback before adding more automation. Broader NPC, vehicle, crane, conveyor and power systems remain outside the implemented prototype.

Completion requires a verified playable build and creator approval. Funding, release date, production platform promises and campaign publication remain undecided and outside this work.
