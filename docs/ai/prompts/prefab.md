# Unity Prefab Prompt

Prefer Unity MCP.
If MCP is unavailable, create a PrefabBuilder Editor Tool.

Rules:
- Never edit `.prefab` YAML directly.
- Never create GUID or `.meta` manually.
- Use PrefabUtility.
- Connect components through Unity.
- Report missing assets.
- Human must validate Collider, references, and visuals.
