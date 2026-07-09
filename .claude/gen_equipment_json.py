import json

G = {
    'Machete': 'bef05be41376b52498b91573c3ff5a3b',
    'Katana': 'c93dc5e6b62380f499e6ec8328bc0160',
    'Bat_Wood': 'd855c6cd18605f34dbfdca25cb40d71c',
    'Crowbar': '8775c006a5395ff488d9315934a2a901',
    'FireAxe': 'cec5944b3fba5e643b7558cbd3366cd4',
    'WoodAxe': '14b9097cb10b27d40a35dc944853603d',
    'Pistol': 'a1608518da2a041418e8a5351d223d94',
    'SubMGun_02': 'e465d39263959404582f9fdf69f40c53',
    'Shotgun': 'a2458a3a2318bb64ea864a87f6558170',
    'AssaultRifle_01': 'ad49a19d2c4adc94b838606a5d459f85',
    'AssaultRifle_02': 'd9f9bf1ca5142254ba3a359ff4a34b3e',
    'HuntingRifle': 'f8ee5a952b0a300448ad8d17e2ab2584',
    'SniperRifle': 'adc01ffa88b3d6744a5eb21a731f6c23',
    'Backpack_Small': 'd2051d753ee09c3479f2236184af0281',
    'Backpack_02': 'cb46091a4e57def4f9b8086e79682f49',
    'Pouch_01': '8bee817d1f4e2974f970db169ffa5a81',
}

def ov(overrides_list):
    return json.dumps({'Overrides': overrides_list}, ensure_ascii=False)

def a_tags(tags):
    return {'Path': 'Common/Tags', 'Value': json.dumps(tags, ensure_ascii=False)}

def slot_json(slot_id, capacity, weight_limit):
    s = {'SlotId': slot_id, 'Capacity': capacity, 'WeightLimit': weight_limit}
    return json.dumps(s, ensure_ascii=False)

def compat_ammo(caliber):
    return {'Path': 'Tags/CompatibleAmmo', 'Value': json.dumps([caliber], ensure_ascii=False)}

def melee_tags(t):
    return ['Entity.Equipment.Weapon.Melee.' + t, 'Grip.Melee.OneHanded']

def ranged_tags(t):
    return ['Entity.Equipment.Weapon.Ranged.' + t]

equip = []

# ==== Armor Set A: Scavenger (5) ====
equip.append({'entityType':'Armor','name':'ScavengerHood','templateName':'HeadArmor','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Head']),
    {'Path':'Base/Weight','Value':'0.5'},{'Path':'Equipment/Durability','Value':'80','Max':'80'},
    {'Path':'Combat/DEF','Value':'3'},{'Path':'Combat/Coverage','Value':'30'},
    {'Path':'Combat/TraumaTransfer','Value':'80'},{'Path':'Penalty/MoveSpeedPenalty','Value':'2'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'2'},{'Path':'Combat/FlashResist','Value':'10'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Head'},
        {'Path': 'Common/Id', 'Value': 'scavenger_hood'},
    ]),'prefabGuid':''})
equip.append({'entityType':'Armor','name':'ScavengerVest','templateName':'BodyArmor','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Chest']),
    {'Path':'Base/Weight','Value':'1.5'},{'Path':'Equipment/Durability','Value':'80','Max':'80'},
    {'Path':'Combat/DEF','Value':'5'},{'Path':'Combat/Coverage','Value':'50'},
    {'Path':'Combat/TraumaTransfer','Value':'70'},{'Path':'Penalty/MoveSpeedPenalty','Value':'5'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'5'},{'Path':'Combat/KnockdownResist','Value':'5'},
    {'Path':'Combat/StanceStability','Value':'5'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Chest'},
        {'Path': 'Common/Id', 'Value': 'scavenger_vest'},
    ]),'prefabGuid':''})
equip.append({'entityType':'Armor','name':'ScavengerPants','templateName':'LegArmor','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Legs']),
    {'Path':'Base/Weight','Value':'0.8'},{'Path':'Equipment/Durability','Value':'80','Max':'80'},
    {'Path':'Combat/DEF','Value':'3'},{'Path':'Combat/Coverage','Value':'60'},
    {'Path':'Combat/TraumaTransfer','Value':'80'},{'Path':'Penalty/MoveSpeedPenalty','Value':'2'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'2'},{'Path':'Combat/MoveSpeed','Value':'5'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Legs'},
        {'Path': 'Common/Id', 'Value': 'scavenger_pants'},
    ]),'prefabGuid':''})
equip.append({'entityType':'Armor','name':'ScavengerSneakers','templateName':'LegArmor','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Feet']),
    {'Path':'Base/Weight','Value':'0.5'},{'Path':'Equipment/Durability','Value':'60','Max':'60'},
    {'Path':'Combat/DEF','Value':'2'},{'Path':'Combat/Coverage','Value':'30'},
    {'Path':'Combat/TraumaTransfer','Value':'90'},{'Path':'Penalty/MoveSpeedPenalty','Value':'0'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'0'},{'Path':'Combat/SneakSpeed','Value':'10'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Feet'},
        {'Path': 'Common/Id', 'Value': 'scavenger_sneakers'},
    ]),'prefabGuid':''})
equip.append({'entityType':'Armor','name':'WorkGloves','templateName':'ArmorBase','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Hands']),
    {'Path':'Base/Weight','Value':'0.2'},{'Path':'Equipment/Durability','Value':'50','Max':'50'},
    {'Path':'Combat/DEF','Value':'2'},{'Path':'Combat/Coverage','Value':'20'},
    {'Path':'Combat/TraumaTransfer','Value':'100'},{'Path':'Penalty/MoveSpeedPenalty','Value':'0'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'0'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Hands'},
        {'Path': 'Common/Id', 'Value': 'work_gloves'},
    ]),'prefabGuid':''})

# ==== Armor Set B: Tactical (5) ====
equip.append({'entityType':'Armor','name':'RiotHelmet','templateName':'HeadArmor','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Head']),
    {'Path':'Base/Weight','Value':'2.5'},{'Path':'Equipment/Durability','Value':'150','Max':'150'},
    {'Path':'Combat/DEF','Value':'8'},{'Path':'Combat/Coverage','Value':'70'},
    {'Path':'Combat/TraumaTransfer','Value':'50'},{'Path':'Penalty/MoveSpeedPenalty','Value':'8'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'5'},{'Path':'Combat/FlashResist','Value':'30'},
    {'Path':'Combat/NightVision','Value':'10'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Head'},
        {'Path': 'Common/Id', 'Value': 'riot_helmet'},
    ]),'prefabGuid':''})
equip.append({'entityType':'Armor','name':'PlateCarrier','templateName':'BodyArmor','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Chest']),
    {'Path':'Base/Weight','Value':'6.0'},{'Path':'Equipment/Durability','Value':'200','Max':'200'},
    {'Path':'Combat/DEF','Value':'22'},{'Path':'Combat/Coverage','Value':'60'},
    {'Path':'Combat/TraumaTransfer','Value':'30'},{'Path':'Penalty/MoveSpeedPenalty','Value':'15'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'10'},{'Path':'Combat/KnockdownResist','Value':'20'},
    {'Path':'Combat/StanceStability','Value':'15'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Chest'},
        {'Path': 'Common/Id', 'Value': 'plate_carrier'},
    ]),'prefabGuid':''})
equip.append({'entityType':'Armor','name':'TacticalKneepad','templateName':'LegArmor','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Legs']),
    {'Path':'Base/Weight','Value':'2.0'},{'Path':'Equipment/Durability','Value':'150','Max':'150'},
    {'Path':'Combat/DEF','Value':'10'},{'Path':'Combat/Coverage','Value':'40'},
    {'Path':'Combat/TraumaTransfer','Value':'60'},{'Path':'Penalty/MoveSpeedPenalty','Value':'5'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'3'},{'Path':'Combat/MoveSpeed','Value':'3'},
    {'Path':'Combat/SneakSpeed','Value':'3'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Legs'},
        {'Path': 'Common/Id', 'Value': 'tactical_kneepad'},
    ]),'prefabGuid':''})
equip.append({'entityType':'Armor','name':'CombatBoots','templateName':'LegArmor','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Feet']),
    {'Path':'Base/Weight','Value':'2.0'},{'Path':'Equipment/Durability','Value':'150','Max':'150'},
    {'Path':'Combat/DEF','Value':'8'},{'Path':'Combat/Coverage','Value':'50'},
    {'Path':'Combat/TraumaTransfer','Value':'50'},{'Path':'Penalty/MoveSpeedPenalty','Value':'4'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'3'},{'Path':'Combat/MoveSpeed','Value':'10'},
    {'Path':'Combat/SneakSpeed','Value':'5'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Feet'},
        {'Path': 'Common/Id', 'Value': 'combat_boots'},
    ]),'prefabGuid':''})
equip.append({'entityType':'Armor','name':'TacticalGloves','templateName':'ArmorBase','overridesJson':ov([
    a_tags(['Entity.Equipment.Armor.Hands']),
    {'Path':'Base/Weight','Value':'0.5'},{'Path':'Equipment/Durability','Value':'100','Max':'100'},
    {'Path':'Combat/DEF','Value':'5'},{'Path':'Combat/Coverage','Value':'30'},
    {'Path':'Combat/TraumaTransfer','Value':'60'},{'Path':'Penalty/MoveSpeedPenalty','Value':'2'},
    {'Path':'Penalty/StaminaRegenPenalty','Value':'0'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Armor.Hands'},
        {'Path': 'Common/Id', 'Value': 'tactical_gloves'},
    ]),'prefabGuid':''})

# ==== Containers (3) ====
equip.append({'entityType':'Container','name':'CanvasBag','templateName':'Backpack','overridesJson':ov([
    a_tags(['Entity.Equipment.Container']),
    {'Path':'Base/Weight','Value':'0.5'},{'Path':'Equipment/Durability','Value':'50','Max':'50'},
    {'Path':'Backpack/CarryWeightBonus','Value':'10'},
    {'Path':'Slots/ContainerSlot','Value':slot_json('ContainerSlot',10,15.0)},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Container'},
        {'Path': 'Common/Id', 'Value': 'canvas_bag'},
    ]),'prefabGuid':G['Backpack_Small']})
equip.append({'entityType':'Container','name':'TacticalBackpack','templateName':'Backpack','overridesJson':ov([
    a_tags(['Entity.Equipment.Container']),
    {'Path':'Base/Weight','Value':'1.5'},{'Path':'Equipment/Durability','Value':'150','Max':'150'},
    {'Path':'Backpack/CarryWeightBonus','Value':'25'},
    {'Path':'Slots/ContainerSlot','Value':slot_json('ContainerSlot',20,35.0)},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Container'},
        {'Path': 'Common/Id', 'Value': 'tactical_backpack'},
    ]),'prefabGuid':G['Backpack_02']})
equip.append({'entityType':'Container','name':'WaistPouch','templateName':'Backpack','overridesJson':ov([
    a_tags(['Entity.Equipment.Container']),
    {'Path':'Base/Weight','Value':'0.3'},{'Path':'Equipment/Durability','Value':'50','Max':'50'},
    {'Path':'Backpack/CarryWeightBonus','Value':'5'},
    {'Path':'Slots/ContainerSlot','Value':slot_json('ContainerSlot',5,8.0)},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Container'},
        {'Path': 'Common/Id', 'Value': 'waist_pouch'},
    ]),'prefabGuid':G['Pouch_01']})

# ==== Melee (6) ====
equip.append({'entityType':'MeleeWeapon','name':'Machete','templateName':'Blade','overridesJson':ov([
    a_tags(melee_tags('Blade')),
    {'Path':'Base/Weight','Value':'1.5'},{'Path':'Equipment/Durability','Value':'100','Max':'100'},
    {'Path':'Weapon/AttackSpeed','Value':'1.2'},{'Path':'Weapon/AttackRange','Value':'1.2'},
    {'Path':'Weapon/NoiseRadius','Value':'10'},{'Path':'Weapon/IsTwoHanded','Value':'false'},
    {'Path':'Combat/BleedChance','Value':'15'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Melee.Blade'},
        {'Path': 'Common/Id', 'Value': 'machete'},
    ]),'prefabGuid':G['Machete']})
equip.append({'entityType':'MeleeWeapon','name':'Katana','templateName':'Blade','overridesJson':ov([
    a_tags(melee_tags('Blade')),
    {'Path':'Base/Weight','Value':'1.2'},{'Path':'Equipment/Durability','Value':'120','Max':'120'},
    {'Path':'Weapon/AttackSpeed','Value':'1.4'},{'Path':'Weapon/AttackRange','Value':'1.5'},
    {'Path':'Weapon/NoiseRadius','Value':'8'},{'Path':'Weapon/IsTwoHanded','Value':'false'},
    {'Path':'Combat/BleedChance','Value':'20'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Melee.Blade'},
        {'Path': 'Common/Id', 'Value': 'katana'},
    ]),'prefabGuid':G['Katana']})
equip.append({'entityType':'MeleeWeapon','name':'BaseballBat','templateName':'Blunt','overridesJson':ov([
    a_tags(melee_tags('Blunt')),
    {'Path':'Base/Weight','Value':'2.0'},{'Path':'Equipment/Durability','Value':'80','Max':'80'},
    {'Path':'Weapon/AttackSpeed','Value':'1.0'},{'Path':'Weapon/AttackRange','Value':'1.3'},
    {'Path':'Weapon/NoiseRadius','Value':'15'},{'Path':'Weapon/IsTwoHanded','Value':'false'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Melee.Blunt'},
        {'Path': 'Common/Id', 'Value': 'baseball_bat'},
    ]),'prefabGuid':G['Bat_Wood']})
equip.append({'entityType':'MeleeWeapon','name':'Crowbar','templateName':'Blunt','overridesJson':ov([
    a_tags(melee_tags('Blunt')),
    {'Path':'Base/Weight','Value':'3.0'},{'Path':'Equipment/Durability','Value':'120','Max':'120'},
    {'Path':'Weapon/AttackSpeed','Value':'0.9'},{'Path':'Weapon/AttackRange','Value':'1.1'},
    {'Path':'Weapon/NoiseRadius','Value':'12'},{'Path':'Weapon/IsTwoHanded','Value':'false'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Melee.Blunt'},
        {'Path': 'Common/Id', 'Value': 'crowbar'},
    ]),'prefabGuid':G['Crowbar']})
equip.append({'entityType':'MeleeWeapon','name':'FireAxe','templateName':'Axe','overridesJson':ov([
    a_tags(melee_tags('Axe')),
    {'Path':'Base/Weight','Value':'4.0'},{'Path':'Equipment/Durability','Value':'120','Max':'120'},
    {'Path':'Weapon/AttackSpeed','Value':'0.7'},{'Path':'Weapon/AttackRange','Value':'1.0'},
    {'Path':'Weapon/NoiseRadius','Value':'20'},{'Path':'Weapon/IsTwoHanded','Value':'true'},
    {'Path':'Combat/BleedChance','Value':'5'},{'Path':'Combat/ArmorPierce','Value':'25'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Melee.Axe'},
        {'Path': 'Common/Id', 'Value': 'fire_axe'},
    ]),'prefabGuid':G['FireAxe']})
equip.append({'entityType':'MeleeWeapon','name':'WoodAxe','templateName':'Axe','overridesJson':ov([
    a_tags(melee_tags('Axe')),
    {'Path':'Base/Weight','Value':'3.0'},{'Path':'Equipment/Durability','Value':'80','Max':'80'},
    {'Path':'Weapon/AttackSpeed','Value':'0.8'},{'Path':'Weapon/AttackRange','Value':'0.8'},
    {'Path':'Weapon/NoiseRadius','Value':'18'},{'Path':'Weapon/IsTwoHanded','Value':'false'},
    {'Path':'Combat/ArmorPierce','Value':'15'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Melee.Axe'},
        {'Path': 'Common/Id', 'Value': 'wood_axe'},
    ]),'prefabGuid':G['WoodAxe']})

# ==== Ranged (7) ====
equip.append({'entityType':'RangedWeapon','name':'M1911','templateName':'Pistol','overridesJson':ov([
    a_tags(ranged_tags('Pistol')+['Grip.Ranged.Pistol2H']),
    {'Path':'Base/Weight','Value':'1.1'},{'Path':'Equipment/Durability','Value':'150','Max':'150'},
    {'Path':'Weapon/NoiseRadius','Value':'40'},{'Path':'Weapon/IsTwoHanded','Value':'false'},
    {'Path':'Combat/Accuracy','Value':'65'},{'Path':'Combat/ReloadSpeed','Value':'1.0'},
    {'Path':'Combat/MagSize','Value':'7'},{'Path':'Combat/AmmoCount','Value':'7'},
    compat_ammo('Entity.Ammo.Caliber.9mm'),
    {'Path':'Combat/FireRate','Value':'3.0'},{'Path':'Combat/BarrelLength','Value':'4'},
    {'Path':'Combat/RecoilModifier','Value':'20'},{'Path':'Combat/Reliability','Value':'90'},
    {'Path':'Firearm/IsAutomatic','Value':'false'},{'Path':'Firearm/GearType','Value':'Pistol'},
    {'Path':'Pistol/HolsterSpeed','Value':'1.5'},{'Path':'Pistol/HipFirePenalty','Value':'15'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Ranged.Pistol'},
        {'Path': 'Common/Id', 'Value': 'm1911'},
    ]),'prefabGuid':G['Pistol']})
equip.append({'entityType':'RangedWeapon','name':'MP5','templateName':'Pistol','overridesJson':ov([
    a_tags(ranged_tags('Pistol')+['Grip.Ranged.Pistol2H']),
    {'Path':'Base/Weight','Value':'3.0'},{'Path':'Equipment/Durability','Value':'200','Max':'200'},
    {'Path':'Weapon/NoiseRadius','Value':'35'},{'Path':'Weapon/IsTwoHanded','Value':'true'},
    {'Path':'Combat/Accuracy','Value':'60'},{'Path':'Combat/ReloadSpeed','Value':'0.8'},
    {'Path':'Combat/MagSize','Value':'30'},{'Path':'Combat/AmmoCount','Value':'30'},
    compat_ammo('Entity.Ammo.Caliber.9mm'),
    {'Path':'Combat/FireRate','Value':'8.0'},{'Path':'Combat/BarrelLength','Value':'6'},
    {'Path':'Combat/RecoilModifier','Value':'15'},{'Path':'Combat/Reliability','Value':'92'},
    {'Path':'Firearm/IsAutomatic','Value':'true'},{'Path':'Firearm/GearType','Value':'SMG'},
    {'Path':'Pistol/HolsterSpeed','Value':'1.0'},{'Path':'Pistol/HipFirePenalty','Value':'10'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Ranged.Pistol'},
        {'Path': 'Common/Id', 'Value': 'mp5'},
    ]),'prefabGuid':G['SubMGun_02']})
equip.append({'entityType':'RangedWeapon','name':'Remington870','templateName':'Shotgun','overridesJson':ov([
    a_tags(ranged_tags('Shotgun')),
    {'Path':'Base/Weight','Value':'3.5'},{'Path':'Equipment/Durability','Value':'200','Max':'200'},
    {'Path':'Weapon/NoiseRadius','Value':'90'},{'Path':'Weapon/IsTwoHanded','Value':'true'},
    {'Path':'Combat/Accuracy','Value':'40'},{'Path':'Combat/ReloadSpeed','Value':'0.5'},
    {'Path':'Combat/MagSize','Value':'5'},{'Path':'Combat/AmmoCount','Value':'5'},
    compat_ammo('Entity.Ammo.Caliber.12ga'),
    {'Path':'Combat/FireRate','Value':'1.0'},{'Path':'Combat/BarrelLength','Value':'18'},
    {'Path':'Combat/RecoilModifier','Value':'50'},{'Path':'Combat/Reliability','Value':'95'},
    {'Path':'Firearm/IsAutomatic','Value':'false'},{'Path':'Firearm/GearType','Value':'Shotgun'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Ranged.Shotgun'},
        {'Path': 'Common/Id', 'Value': 'remington870'},
    ]),'prefabGuid':G['Shotgun']})
equip.append({'entityType':'RangedWeapon','name':'AK47','templateName':'Rifle','overridesJson':ov([
    a_tags(ranged_tags('Rifle')),
    {'Path':'Base/Weight','Value':'4.3'},{'Path':'Equipment/Durability','Value':'300','Max':'300'},
    {'Path':'Weapon/NoiseRadius','Value':'85'},{'Path':'Weapon/IsTwoHanded','Value':'true'},
    {'Path':'Combat/Accuracy','Value':'75'},{'Path':'Combat/ReloadSpeed','Value':'0.7'},
    {'Path':'Combat/MagSize','Value':'30'},{'Path':'Combat/AmmoCount','Value':'30'},
    compat_ammo('Entity.Ammo.Caliber.762'),
    {'Path':'Combat/FireRate','Value':'5.0'},{'Path':'Combat/BarrelLength','Value':'16'},
    {'Path':'Combat/RecoilModifier','Value':'35'},{'Path':'Combat/Reliability','Value':'95'},
    {'Path':'Firearm/IsAutomatic','Value':'true'},{'Path':'Firearm/GearType','Value':'Rifle'},
    {'Path':'Rifle/AimTime','Value':'0.8'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Ranged.Rifle'},
        {'Path': 'Common/Id', 'Value': 'ak47'},
    ]),'prefabGuid':G['AssaultRifle_01']})
equip.append({'entityType':'RangedWeapon','name':'M4A1','templateName':'Rifle','overridesJson':ov([
    a_tags(ranged_tags('Rifle')),
    {'Path':'Base/Weight','Value':'3.5'},{'Path':'Equipment/Durability','Value':'300','Max':'300'},
    {'Path':'Weapon/NoiseRadius','Value':'80'},{'Path':'Weapon/IsTwoHanded','Value':'true'},
    {'Path':'Combat/Accuracy','Value':'85'},{'Path':'Combat/ReloadSpeed','Value':'0.7'},
    {'Path':'Combat/MagSize','Value':'30'},{'Path':'Combat/AmmoCount','Value':'30'},
    compat_ammo('Entity.Ammo.Caliber.556'),
    {'Path':'Combat/FireRate','Value':'6.0'},{'Path':'Combat/BarrelLength','Value':'14'},
    {'Path':'Combat/RecoilModifier','Value':'30'},{'Path':'Combat/Reliability','Value':'96'},
    {'Path':'Firearm/IsAutomatic','Value':'true'},{'Path':'Firearm/GearType','Value':'Rifle'},
    {'Path':'Rifle/AimTime','Value':'0.6'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Ranged.Rifle'},
        {'Path': 'Common/Id', 'Value': 'm4a1'},
    ]),'prefabGuid':G['AssaultRifle_02']})
equip.append({'entityType':'RangedWeapon','name':'HuntingRifle','templateName':'Rifle','overridesJson':ov([
    a_tags(ranged_tags('Rifle')),
    {'Path':'Base/Weight','Value':'4.0'},{'Path':'Equipment/Durability','Value':'200','Max':'200'},
    {'Path':'Weapon/NoiseRadius','Value':'75'},{'Path':'Weapon/IsTwoHanded','Value':'true'},
    {'Path':'Combat/Accuracy','Value':'90'},{'Path':'Combat/ReloadSpeed','Value':'0.4'},
    {'Path':'Combat/MagSize','Value':'5'},{'Path':'Combat/AmmoCount','Value':'5'},
    compat_ammo('Entity.Ammo.Caliber.762'),
    {'Path':'Combat/FireRate','Value':'1.5'},{'Path':'Combat/BarrelLength','Value':'22'},
    {'Path':'Combat/RecoilModifier','Value':'30'},{'Path':'Combat/Reliability','Value':'92'},
    {'Path':'Firearm/IsAutomatic','Value':'false'},{'Path':'Firearm/GearType','Value':'Rifle'},
    {'Path':'Rifle/AimTime','Value':'1.2'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Ranged.Rifle'},
        {'Path': 'Common/Id', 'Value': 'hunting_rifle'},
    ]),'prefabGuid':G['HuntingRifle']})
equip.append({'entityType':'RangedWeapon','name':'SVD','templateName':'Rifle','overridesJson':ov([
    a_tags(ranged_tags('Rifle')),
    {'Path':'Base/Weight','Value':'4.5'},{'Path':'Equipment/Durability','Value':'250','Max':'250'},
    {'Path':'Weapon/NoiseRadius','Value':'90'},{'Path':'Weapon/IsTwoHanded','Value':'true'},
    {'Path':'Combat/Accuracy','Value':'92'},{'Path':'Combat/ReloadSpeed','Value':'0.5'},
    {'Path':'Combat/MagSize','Value':'10'},{'Path':'Combat/AmmoCount','Value':'10'},
    compat_ammo('Entity.Ammo.Caliber.762'),
    {'Path':'Combat/FireRate','Value':'2.0'},{'Path':'Combat/BarrelLength','Value':'20'},
    {'Path':'Combat/RecoilModifier','Value':'38'},{'Path':'Combat/Reliability','Value':'93'},
    {'Path':'Firearm/IsAutomatic','Value':'false'},{'Path':'Firearm/GearType','Value':'Rifle'},
    {'Path':'Rifle/AimTime','Value':'1.0'},
        {'Path': 'Common/Category', 'Value': 'Entity.Equipment.Weapon.Ranged.Rifle'},
        {'Path': 'Common/Id', 'Value': 'svd'},
    ]),'prefabGuid':G['SniperRifle']})

# Write
data = {'version':'1.0','description':'S5 Item Economy — Equipment (Armor + Container + Melee + Ranged).','category':'Equipment','entities':equip}
with open('Assets/Data/Entities/Equipment/equipment_all.json','w',encoding='utf-8') as f:
    f.write(json.dumps(data,indent=2,ensure_ascii=False))
print(f'OK: equipment_all.json — {len(equip)} entities')
