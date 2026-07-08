import json

G = {
    'Can_01': '43c02ad0264295844bda05a0e56a7b21',
    'Bread': 'ea8fc872ca6a2dd43aff8e9c1cfe6b6d',
    'Meat_Cooked': '49aef79c0f8b14f409456b1e709dee02',
    'Mushroom': 'dd5b1361103cd5645bbabbef12b1a8f7',
    'Drink_Bottle': '6f4046d7967c3b74a88971615df168e4',
    'Drink': '907f8303af992f44289b29cb614b45c5',
    'Alcohol': '58ee3df2b81b0de49a953371616e8154',
    'Tape': 'e228fc92e8af2974b932665a3d01b206',
    'Shop_Goods': '55e31d9d98e7506428bd5fc800ea4122',
    'Pills': '07973e2f008580343949b7f8c06cf72c',
}

def ov(overrides_list):
    return json.dumps({'Overrides': overrides_list}, ensure_ascii=False)

def a_tags(tags):
    return {'Path': 'Common/Tags', 'Value': json.dumps(tags, ensure_ascii=False)}

entities = []

# ==== Keep & transform: CannedFood → CannedBeans ====
entities.append({'entityType':'Consumable','name':'CannedBeans','templateName':'Food','overridesJson':ov([
    a_tags(['Entity.Consumable.Food']),
    {'Path':'Tags/ConsumableType','Value':'Food'},
    {'Path':'Tags/FoodType','Value':'Canned'},
    {'Path':'Base/StackSize','Value':'10'},
    {'Path':'Base/ConsumeTime','Value':'2.0'},
    {'Path':'Nutrition/Nutrition_Val','Value':'25'},
    {'Path':'Nutrition/Hydration','Value':'5'},
    {'Path':'Quality/MoraleBonus','Value':'3'},
    {'Path':'Quality/ShelfLife','Value':'999'},
    {'Path':'Quality/ContaminationRisk','Value':'0'},
]),'prefabGuid':G['Can_01']})

# ==== Keep & transform: Bandage → complete ====
entities.append({'entityType':'Consumable','name':'Bandage','templateName':'Medical','overridesJson':ov([
    a_tags(['Entity.Consumable.Medical']),
    {'Path':'Tags/ConsumableType','Value':'Medical'},
    {'Path':'Tags/MedicalType','Value':'Bandage'},
    {'Path':'Base/StackSize','Value':'20'},
    {'Path':'Base/ConsumeTime','Value':'3.0'},
    {'Path':'Heal/HealAmount','Value':'10'},
    {'Path':'Heal/BleedReduction','Value':'100'},
    {'Path':'Heal/InfectionCleanse','Value':'0'},
    {'Path':'Heal/PainRelief','Value':'0'},
    {'Path':'Heal/HealDuration','Value':'5.0'},
]),'prefabGuid':G['Tape']})

# ==== Food (3 new) ====
entities.append({'entityType':'Consumable','name':'Bread','templateName':'Food','overridesJson':ov([
    a_tags(['Entity.Consumable.Food']),
    {'Path':'Tags/ConsumableType','Value':'Food'},
    {'Path':'Tags/FoodType','Value':'Grain'},
    {'Path':'Base/StackSize','Value':'5'},
    {'Path':'Base/ConsumeTime','Value':'1.5'},
    {'Path':'Nutrition/Nutrition_Val','Value':'20'},
    {'Path':'Nutrition/Hydration','Value':'0'},
    {'Path':'Quality/MoraleBonus','Value':'5'},
    {'Path':'Quality/ShelfLife','Value':'3'},
    {'Path':'Quality/ContaminationRisk','Value':'5'},
]),'prefabGuid':G['Bread']})
entities.append({'entityType':'Consumable','name':'CookedMeat','templateName':'Food','overridesJson':ov([
    a_tags(['Entity.Consumable.Food']),
    {'Path':'Tags/ConsumableType','Value':'Food'},
    {'Path':'Tags/FoodType','Value':'Meat'},
    {'Path':'Base/StackSize','Value':'5'},
    {'Path':'Base/ConsumeTime','Value':'3.0'},
    {'Path':'Nutrition/Nutrition_Val','Value':'30'},
    {'Path':'Nutrition/Hydration','Value':'0'},
    {'Path':'Quality/MoraleBonus','Value':'8'},
    {'Path':'Quality/ShelfLife','Value':'2'},
    {'Path':'Quality/ContaminationRisk','Value':'10'},
]),'prefabGuid':G['Meat_Cooked']})
entities.append({'entityType':'Consumable','name':'Mushroom','templateName':'Food','overridesJson':ov([
    a_tags(['Entity.Consumable.Food']),
    {'Path':'Tags/ConsumableType','Value':'Food'},
    {'Path':'Tags/FoodType','Value':'Vegetable'},
    {'Path':'Base/StackSize','Value':'10'},
    {'Path':'Base/ConsumeTime','Value':'1.0'},
    {'Path':'Nutrition/Nutrition_Val','Value':'8'},
    {'Path':'Nutrition/Hydration','Value':'3'},
    {'Path':'Quality/MoraleBonus','Value':'2'},
    {'Path':'Quality/ShelfLife','Value':'2'},
    {'Path':'Quality/ContaminationRisk','Value':'15'},
]),'prefabGuid':G['Mushroom']})

# ==== Drinks (3) ====
entities.append({'entityType':'Consumable','name':'BottledWater','templateName':'Food','overridesJson':ov([
    a_tags(['Entity.Consumable.Food']),
    {'Path':'Tags/ConsumableType','Value':'Food'},
    {'Path':'Tags/FoodType','Value':'Drink'},
    {'Path':'Base/StackSize','Value':'10'},
    {'Path':'Base/ConsumeTime','Value':'1.0'},
    {'Path':'Nutrition/Nutrition_Val','Value':'0'},
    {'Path':'Nutrition/Hydration','Value':'40'},
    {'Path':'Quality/MoraleBonus','Value':'2'},
    {'Path':'Quality/ShelfLife','Value':'999'},
    {'Path':'Quality/ContaminationRisk','Value':'0'},
]),'prefabGuid':G['Drink_Bottle']})
entities.append({'entityType':'Consumable','name':'Soda','templateName':'Food','overridesJson':ov([
    a_tags(['Entity.Consumable.Food']),
    {'Path':'Tags/ConsumableType','Value':'Food'},
    {'Path':'Tags/FoodType','Value':'Drink'},
    {'Path':'Base/StackSize','Value':'10'},
    {'Path':'Base/ConsumeTime','Value':'1.0'},
    {'Path':'Nutrition/Nutrition_Val','Value':'5'},
    {'Path':'Nutrition/Hydration','Value':'30'},
    {'Path':'Quality/MoraleBonus','Value':'8'},
    {'Path':'Quality/ShelfLife','Value':'999'},
    {'Path':'Quality/ContaminationRisk','Value':'0'},
]),'prefabGuid':G['Drink']})
entities.append({'entityType':'Consumable','name':'Beer','templateName':'Food','overridesJson':ov([
    a_tags(['Entity.Consumable.Food']),
    {'Path':'Tags/ConsumableType','Value':'Food'},
    {'Path':'Tags/FoodType','Value':'Drink'},
    {'Path':'Base/StackSize','Value':'10'},
    {'Path':'Base/ConsumeTime','Value':'2.0'},
    {'Path':'Nutrition/Nutrition_Val','Value':'5'},
    {'Path':'Nutrition/Hydration','Value':'20'},
    {'Path':'Quality/MoraleBonus','Value':'10'},
    {'Path':'Quality/ShelfLife','Value':'999'},
    {'Path':'Quality/ContaminationRisk','Value':'0'},
]),'prefabGuid':G['Alcohol']})

# ==== Medical (3 new) ====
entities.append({'entityType':'Consumable','name':'FirstAidKit','templateName':'Medical','overridesJson':ov([
    a_tags(['Entity.Consumable.Medical']),
    {'Path':'Tags/ConsumableType','Value':'Medical'},
    {'Path':'Tags/MedicalType','Value':'FirstAid'},
    {'Path':'Base/StackSize','Value':'5'},
    {'Path':'Base/ConsumeTime','Value':'5.0'},
    {'Path':'Heal/HealAmount','Value':'40'},
    {'Path':'Heal/BleedReduction','Value':'100'},
    {'Path':'Heal/InfectionCleanse','Value':'50'},
    {'Path':'Heal/PainRelief','Value':'20'},
    {'Path':'Heal/HealDuration','Value':'8.0'},
]),'prefabGuid':G['Shop_Goods']})
entities.append({'entityType':'Consumable','name':'Painkiller','templateName':'Medical','overridesJson':ov([
    a_tags(['Entity.Consumable.Medical']),
    {'Path':'Tags/ConsumableType','Value':'Medical'},
    {'Path':'Tags/MedicalType','Value':'Drug'},
    {'Path':'Base/StackSize','Value':'10'},
    {'Path':'Base/ConsumeTime','Value':'1.0'},
    {'Path':'Heal/HealAmount','Value':'0'},
    {'Path':'Heal/BleedReduction','Value':'0'},
    {'Path':'Heal/InfectionCleanse','Value':'0'},
    {'Path':'Heal/PainRelief','Value':'80'},
    {'Path':'Heal/HealDuration','Value':'120.0'},
]),'prefabGuid':G['Pills']})
entities.append({'entityType':'Consumable','name':'Antibiotic','templateName':'Medical','overridesJson':ov([
    a_tags(['Entity.Consumable.Medical']),
    {'Path':'Tags/ConsumableType','Value':'Medical'},
    {'Path':'Tags/MedicalType','Value':'Drug'},
    {'Path':'Base/StackSize','Value':'5'},
    {'Path':'Base/ConsumeTime','Value':'1.0'},
    {'Path':'Heal/HealAmount','Value':'0'},
    {'Path':'Heal/BleedReduction','Value':'0'},
    {'Path':'Heal/InfectionCleanse','Value':'100'},
    {'Path':'Heal/PainRelief','Value':'0'},
    {'Path':'Heal/HealDuration','Value':'10.0'},
]),'prefabGuid':G['Pills']})

data = {'version':'1.0','description':'S5 Item Economy — Consumables (Food + Drink + Medical).','category':'Consumable','entities':entities}
with open('Assets/Data/Entities/Consumable/consumable_all.json','w',encoding='utf-8') as f:
    f.write(json.dumps(data,indent=2,ensure_ascii=False))
print(f'OK: consumable_all.json — {len(entities)} entities')
