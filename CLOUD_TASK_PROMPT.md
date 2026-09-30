Continue SCRAPSHIFT in this existing repository. Read AGENTS.md, PROJECT_HANDOFF.md, and README.md first, then inspect current changes. Preserve Campaign/ and existing game systems.

A first Unity/C# wire-loop implementation now exists under Assets/Scrapshift. It targets Unity 6000.3.25f1 and URP 17.3.0. Do not scaffold another project. The editor menu Scrapshift → Create or Open Prototype generates the scene, render assets, and balance data without overwriting existing generated scene edits.

The source implements first-person movement/carrying, renewable wire supply, manual stripping, copper sales, a purchasable powered stripper, HUD/pause, and consistent yard-state persistence. Cloud compiled the pure C# model and passed 12 scenarios. Unity import, compilation, save serialization, visuals, and actual playability are still unverified.

The next task is engine integration and verification: open with the pinned editor, resolve packages, run the scene setup command, run 14 EditMode cases, play through the checklist in README, then build a desktop player. Fix observed integration issues and record evidence. Commit verified generated assets with their .meta files and the package lockfile as appropriate. If Unity remains unavailable, state that explicitly and make useful source/core-test progress without claiming editor validation.

Stay focused on the manual-to-automatic wire loop before adding appliance repairs or conveyors. Keep balance editable, bounded inventory, exactly-once material transactions, and consistent ownership/inventory/processing saves. Update PROJECT_HANDOFF.md after meaningful work.

No Kickstarter publication, account changes, financial commitments, or generated artwork presented as gameplay are part of development.
