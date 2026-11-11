# Unity Templates

UnityTemplates supplies ordinary source modules, not UPM packages. On Windows, keep the sibling checkout at
`../UnityTemplates`, close the Unity Editor, and run `powershell -ExecutionPolicy Bypass -File Tools/Link-UnityTemplates.ps1`.
The script creates directory junctions at `Assets/TemplateSources/Foundation` and `Assets/TemplateSources/Modules`.
The junctions are ignored by Git; their folder metadata is committed. Source assets and their GUIDs remain in
UnityTemplates, and edits there are live in this project. The script refuses to replace existing directories or links.

The whole Foundation and Modules trees are visible to Unity, including their optional demos and tests. Game assemblies
reference only the modules they use; Haptics remains unused. Unity's Input System, Addressables, and Localization
packages stay in the project manifest for the game and template demos. Changes to shared source require the approval and
workflow described in AGENTS.md.
