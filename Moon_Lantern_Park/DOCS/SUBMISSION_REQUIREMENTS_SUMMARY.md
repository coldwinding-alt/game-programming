# Submission Requirements Summary

Reviewed on: `2026-06-16`

This file summarises the coursework assessment and submission requirements relevant to repository organisation, asset provenance, and final delivery. It is a project-facing checklist, not a replacement for the official module brief.

## Assessment Areas

| Area | Weight | What the repository should help prove |
| --- | ---: | --- |
| Game Concept and Design | 20% | Clear idea, realistic scope, design reasoning, tool/resource plan, legal/ethical/accessibility/security awareness, and development plan |
| Final Game | 60% | Playable vertical slice, stable build, good use of game systems, technical decisions, testing/debugging evidence, report reflection, and live demo/presentation |
| Professionalism | 20% | Consistent development record, organised repository, GitHub history, planning evidence, response to feedback, testing records, and clear asset/AI declarations |

## Final Game Submission Checklist

The final game submission should include:

- Final submission form: `StudentID_CW2_FinalSubmissionForm.pdf`
- Final report: `StudentID_CW2_FinalReport.pdf`
- Final playable build: `StudentID_CW2_FinalGameBuild.zip`, or a clear download link if too large
- Demo video link file if required: `StudentID_CW2_DemoVideo.txt`
- GitHub repository link
- Final commit hash
- Clear instructions for how to run the game
- Controls and main objective
- Win/lose/completion condition
- Main systems/scripts created
- External assets/templates/tutorials/AI used
- Known issues

## Final Report Checklist

The report should explain:

- Design choices
- Technical decisions
- Problems and limitations
- Testing and changes made because of testing
- How the game developed from concept to final version
- Personal contribution
- Use of templates, assets, tutorials, or AI support

## Professionalism Portfolio Checklist

The professionalism portfolio should include:

- GitHub repository link
- Development log showing progress over time
- Summary of important commits
- Planning and task-management evidence
- Evidence of response to feedback
- Testing log and bug-fixing evidence
- Screenshots or short progress evidence over time
- Explanation of how the project changed during development
- External assets/templates/tutorials/AI declaration
- Credits and licences where relevant
- Reflection on organisation, time management, independent work, and professionalism
- Known limitations and how they were managed

## External Resource Declaration Checklist

For every external resource, record:

- Resource name
- Type, such as asset, template, tutorial, AI, code snippet, audio, image, model, or other
- Source
- Licence or permission
- What it provided
- What was used unchanged
- What was modified
- What was created by the student/project
- Where it appears in the game
- How it is credited

For AI assistance, record:

- Tool used
- What was asked
- What output was used
- What was changed
- How it was tested
- What is understood
- What is not fully understood
- Where it appears in the project

The project-specific asset declaration is maintained in:

- `ASSET_CREDITS.md`
- `DOCS/IMAGE_ASSET_PROVENANCE.md`
- `DOCS/AUDIO_COPYRIGHT.md`
- `DOCS/FONT_PROVENANCE.md`
- `DOCS/AI_AND_EXTERNAL_RESOURCE_DECLARATION.md`

## Repository Cleanup Checklist

Before final submission:

- Keep `README.md` clear about the game, controls, Unity version, how to run, and credits.
- Keep `TEST_LOG.md` as evidence of testing, debugging, and improvement.
- Keep asset provenance documents up to date.
- Keep license files for bundled fonts under `DOCS/FontLicenses/`.
- Include local asset-generation scripts when they are cited as provenance evidence.
- Do not submit Unity cache folders such as `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, or build cache folders unless explicitly required.
- Submit a playable build or a clear playable-build download link; do not rely only on a Unity project folder.

## Project-Specific Notes

- The project uses Unity Editor `2022.3.62f3c1`.
- The main scene is `Assets/Scenes/Main.unity`.
- Runtime entry is handled by `mlpAutoBoot`.
- Project image assets are declared as ChatGPT image-generation based assets edited, composited, procedurally exported, or integrated by the project author.
- The background music is declared as CC0 from OpenGameArt.
- Runtime fonts are documented with upstream sources, SHA256 hashes, and OFL license files.
