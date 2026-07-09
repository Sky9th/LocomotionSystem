import json

G = {
    'Ammo_9mm': '86bb9dc1b51ac1b40881bebab79f89cf',
    'Ammo_556': 'f0fc41b28f2a6a943a204ab9ba84bc87',
    'Ammo_762': '3c04b696e75f45847b55a0268a323a4a',
    'Ammo_12G': '3b0fb3fda92d12a4caa1fd3a00956fd1',
}

def ov(overrides_list):
    return json.dumps({'Overrides': overrides_list}, ensure_ascii=False)

def a_tags(tags):
    return {'Path': 'Common/Tags', 'Value': json.dumps(tags, ensure_ascii=False)}

def ammo_common(dmg, pen, noise, bullet_wt, over_pen, recoil, muzzle_vel=None, fouling=None):
    o = [
        {'Path': 'Combat/BaseDamage', 'Value': str(dmg)},
        {'Path': 'Combat/Penetration', 'Value': str(pen)},
        {'Path': 'Combat/NoiseRadius', 'Value': str(noise)},
        {'Path': 'Combat/BulletWeight', 'Value': str(bullet_wt)},
        {'Path': 'Combat/OverPenetration', 'Value': str(over_pen)},
        {'Path': 'Combat/RecoilFactor', 'Value': str(recoil)},
        {'Path': 'Combat/AmmoReliability', 'Value': '100'},
        {'Path': 'Combat/FoulingRate', 'Value': str(fouling if fouling else '1.0')},
    ]
    if muzzle_vel:
        o.append({'Path': 'Combat/MuzzleVelocity', 'Value': str(muzzle_vel)})
    return o

entities = []

# ==== Existing 3 (caliber bases) ====
entities.append({'entityType':None,'name':'PistolAmmo','templateName':'PistolAmmo','overridesJson':ov([]),'prefabGuid':G['Ammo_9mm']})
entities.append({'entityType':None,'name':'RifleAmmo','templateName':'RifleAmmo','overridesJson':ov([]),'prefabGuid':G['Ammo_556']})
entities.append({'entityType':None,'name':'ShotgunShell','templateName':'ShotgunShell','overridesJson':ov([]),'prefabGuid':G['Ammo_12G']})

# ==== 9mm variants (3) ====
entities.append({'entityType':None,'name':'9mm_FMJ','templateName':'PistolAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.9mm']),
    {'Path':'Base/Weight','Value':'0.01'},
    *ammo_common(15,2,40,115,45,1.0,muzzle_vel=360,fouling=1.0),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.9mm'},
        {'Path': 'Common/Id', 'Value': 'nine_mm_fmj'},
    ]),'prefabGuid':G['Ammo_9mm']})
entities.append({'entityType':None,'name':'9mm_JHP','templateName':'PistolAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.9mm']),
    {'Path':'Base/Weight','Value':'0.011'},
    *ammo_common(22,1,40,147,10,1.2,muzzle_vel=330,fouling=1.0),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.9mm'},
        {'Path': 'Common/Id', 'Value': 'nine_mm_jhp'},
    ]),'prefabGuid':G['Ammo_9mm']})
entities.append({'entityType':None,'name':'9mm_Subsonic','templateName':'PistolAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.9mm']),
    {'Path':'Base/Weight','Value':'0.012'},
    *ammo_common(13,2,20,147,35,0.7,muzzle_vel=280,fouling=1.5),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.9mm'},
        {'Path': 'Common/Id', 'Value': 'nine_mm_subsonic'},
    ]),'prefabGuid':G['Ammo_9mm']})

# ==== 5.56 variants (3) ====
entities.append({'entityType':None,'name':'556_FMJ','templateName':'RifleAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.556']),
    {'Path':'Base/Weight','Value':'0.012'},
    *ammo_common(35,8,80,62,60,1.0,muzzle_vel=940,fouling=1.0),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.556'},
        {'Path': 'Common/Id', 'Value': '556_fmj'},
    ]),'prefabGuid':G['Ammo_556']})
entities.append({'entityType':None,'name':'556_AP','templateName':'RifleAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.556']),
    {'Path':'Base/Weight','Value':'0.012'},
    *ammo_common(28,14,85,62,95,1.1,muzzle_vel=910,fouling=1.3),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.556'},
        {'Path': 'Common/Id', 'Value': '556_ap'},
    ]),'prefabGuid':G['Ammo_556']})
entities.append({'entityType':None,'name':'556_HP','templateName':'RifleAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.556']),
    {'Path':'Base/Weight','Value':'0.011'},
    *ammo_common(50,4,80,55,15,0.9,muzzle_vel=950,fouling=1.0),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.556'},
        {'Path': 'Common/Id', 'Value': '556_hp'},
    ]),'prefabGuid':G['Ammo_556']})

# ==== 7.62 variants (3) ====
entities.append({'entityType':None,'name':'762_FMJ','templateName':'RifleAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.762']),
    {'Path':'Base/Weight','Value':'0.016'},
    *ammo_common(42,10,85,123,65,1.0,muzzle_vel=730,fouling=1.0),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.762'},
        {'Path': 'Common/Id', 'Value': '762_fmj'},
    ]),'prefabGuid':G['Ammo_762']})
entities.append({'entityType':None,'name':'762_AP','templateName':'RifleAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.762']),
    {'Path':'Base/Weight','Value':'0.018'},
    *ammo_common(35,16,90,150,98,1.2,muzzle_vel=700,fouling=1.3),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.762'},
        {'Path': 'Common/Id', 'Value': '762_ap'},
    ]),'prefabGuid':G['Ammo_762']})
entities.append({'entityType':None,'name':'762_Subsonic','templateName':'RifleAmmo','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.762']),
    {'Path':'Base/Weight','Value':'0.022'},
    *ammo_common(36,9,30,200,50,0.7,muzzle_vel=310,fouling=1.8),
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.762'},
        {'Path': 'Common/Id', 'Value': '762_subsonic'},
    ]),'prefabGuid':G['Ammo_762']})

# ==== 12ga variants (3) ====
entities.append({'entityType':None,'name':'12ga_Buck','templateName':'ShotgunShell','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.12ga']),
    {'Path':'Base/Weight','Value':'0.04'},
    *ammo_common(50,4,90,438,30,1.0,muzzle_vel=400,fouling=1.2),
    {'Path':'Combat/PelletCount','Value':'8'},
    {'Path':'Combat/Spread','Value':'12'},
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.12ga'},
        {'Path': 'Common/Id', 'Value': 'twelve_ga_buck'},
    ]),'prefabGuid':G['Ammo_12G']})
entities.append({'entityType':None,'name':'12ga_Slug','templateName':'ShotgunShell','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.12ga']),
    {'Path':'Base/Weight','Value':'0.05'},
    *ammo_common(75,10,95,438,70,1.5,muzzle_vel=480,fouling=1.2),
    {'Path':'Combat/PelletCount','Value':'1'},
    {'Path':'Combat/Spread','Value':'2'},
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.12ga'},
        {'Path': 'Common/Id', 'Value': 'twelve_ga_slug'},
    ]),'prefabGuid':G['Ammo_12G']})
entities.append({'entityType':None,'name':'12ga_Breach','templateName':'ShotgunShell','overridesJson':ov([
    a_tags(['Entity.Ammo.Caliber.12ga']),
    {'Path':'Base/Weight','Value':'0.045'},
    *ammo_common(30,15,100,438,20,1.3,muzzle_vel=350,fouling=2.0),
    {'Path':'Combat/PelletCount','Value':'1'},
    {'Path':'Combat/Spread','Value':'1'},
        {'Path': 'Common/Category', 'Value': 'Entity.Entity.Ammo.Caliber.12ga'},
        {'Path': 'Common/Id', 'Value': 'twelve_ga_breach'},
    ]),'prefabGuid':G['Ammo_12G']})

data = {'version':'1.0','description':'S5 Item Economy — Ammo variants (4 calibers x 3 types + 3 bases).','category':'Ammo','entities':entities}
with open('Assets/Data/Entities/Ammo/ammo_all.json','w',encoding='utf-8') as f:
    f.write(json.dumps(data,indent=2,ensure_ascii=False))
print(f'OK: ammo_all.json — {len(entities)} entities')
