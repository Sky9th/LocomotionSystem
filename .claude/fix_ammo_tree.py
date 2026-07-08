import json

# Build the complete AmmoBase treeJson from scratch (existing nodes + new Weapon/ATK)
tree = {
    "Nodes": [
        # Existing Base folder
        {"NodeId": "Base", "ParentId": "", "DefId": ""},
        {"NodeId": "Ammo", "ParentId": "Base", "DefId": "Ammo"},
        # Existing Combat folder
        {"NodeId": "Combat", "ParentId": "", "DefId": ""},
        {"NodeId": "BaseDamage", "ParentId": "Combat", "DefId": "BaseDamage"},
        {"NodeId": "Penetration", "ParentId": "Combat", "DefId": "Penetration"},
        {"NodeId": "BulletWeight", "ParentId": "Combat", "DefId": "BulletWeight"},
        {"NodeId": "OverPenetration", "ParentId": "Combat", "DefId": "OverPenetration"},
        {"NodeId": "NoiseRadius", "ParentId": "Combat", "DefId": "NoiseRadius"},
        {"NodeId": "MuzzleVelocity", "ParentId": "Combat", "DefId": "MuzzleVelocity"},
        {"NodeId": "RecoilFactor", "ParentId": "Combat", "DefId": "RecoilFactor"},
        {"NodeId": "AmmoReliability", "ParentId": "Combat", "DefId": "AmmoReliability"},
        {"NodeId": "FoulingRate", "ParentId": "Combat", "DefId": "FoulingRate"},
        # Existing Tags folder
        {"NodeId": "Tags", "ParentId": "", "DefId": ""},
        {"NodeId": "DamageType", "ParentId": "Tags", "DefId": "DamageType"},
        {"NodeId": "Platform", "ParentId": "Tags", "DefId": "Platform"},
        # NEW Weapon/ATK
        {"NodeId": "Weapon", "ParentId": "", "DefId": ""},
        {"NodeId": "ATK", "ParentId": "Weapon", "DefId": "ATK"},
    ]
}

# Serialize: treeJson value format used by Unity
# Step 1: JSON string with indent=4
inner = json.dumps(tree, indent=4, ensure_ascii=False)
# Step 2: Wrap as JSON string (with escapes)
wrapped = json.dumps(inner, ensure_ascii=False)

# Read the file and find+replace the treeJson line
with open("Assets/Data/Properties/Trees/AmmoBase.asset", 'r', encoding='utf-8') as f:
    content = f.read()

# Find the treeJson line start and end
idx = content.find('treeJson: "')
if idx < 0:
    print("ERROR: no treeJson found")
    exit(1)

# Find end of this YAML field (next line starting with a non-whitespace YAML key)
line_start = content.rfind('\n', 0, idx) + 1
line_end = content.find('\n', idx)
if line_end < 0:
    line_end = len(content)

# Replace just the treeJson line
new_line = '  treeJson: ' + wrapped + '\n'
new_content = content[:line_start] + new_line + content[line_end+1:]

with open("Assets/Data/Properties/Trees/AmmoBase.asset", 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"Total nodes: {len(tree['Nodes'])} (added Weapon, ATK)")
print("Done: AmmoBase.asset updated")
