# 完整示例 — 数据模组

一个完整的数据模组示例：**"增强科技扩展包"**

## 目录结构

```
EnhancedTechMod/
├── mod.json
├── data/
│   ├── balance.json
│   ├── components.json
│   ├── techs.json
│   ├── events.json
│   └── recipes.json
└── locale/
    └── zh-CN.json
```

## mod.json

```json
{
  "id": "enhanced_tech_mod",
  "name": "增强科技扩展包",
  "version": "1.0.0",
  "author": "TechMaster",
  "description": "添加新的科技树分支、组件和合成配方",
  "type": "data",
  "compatibility": {
    "game_version": ">=1.0.0"
  }
}
```

## 平衡调整 (data/balance.json)

```json
{
  "balance": {
    "economy": {
      "base_income": 12,
      "income_per_pop": 0.6,
      "starting_funds": 300
    },
    "research": {
      "base_cost_multiplier": 0.9,
      "cost_per_tech": 40,
      "discount_per_era": 0.15
    }
  }
}
```

## 自定义组件 (data/components.json)

```json
{
  "components": [
    {
      "id": "quantum_core",
      "name": "量子核心",
      "description": "利用量子计算优化所有系统",
      "type": "special",
      "rarity": "legendary",
      "tier": 4,
      "stats": {
        "energy_output": 50,
        "computing_power": 200,
        "heat_generation": 10
      },
      "requirements": {
        "tech": "quantum_computing",
        "level": 2,
        "resources": {
          "titanium": 200,
          "uranium": 100,
          "credits": 10000
        }
      },
      "effects": [
        {
          "type": "passive",
          "effect": "research_speed",
          "value": 0.25
        },
        {
          "type": "passive",
          "effect": "energy_efficiency",
          "value": 0.15
        }
      ],
      "tags": ["quantum", "computing", "legendary"]
    },
    {
      "id": "plasma_cannon_mk2",
      "name": "等离子炮 Mk2",
      "description": "改进型等离子武器，具有更高的射速和穿透力",
      "type": "weapon",
      "rarity": "epic",
      "tier": 3,
      "stats": {
        "damage": 85,
        "fire_rate": 0.8,
        "range": 350,
        "armor_penetration": 0.4
      },
      "requirements": {
        "tech": "plasma_weapons",
        "level": 3,
        "resources": {
          "titanium": 80,
          "energy": 500,
          "credits": 3000
        }
      },
      "effects": [
        {
          "type": "passive",
          "effect": "splash_damage",
          "value": 0.3
        }
      ],
      "tags": ["weapon", "plasma", "epic"]
    }
  ]
}
```

## 自定义科技 (data/techs.json)

```json
{
  "techs": [
    {
      "id": "quantum_computing",
      "name": "量子计算",
      "description": "利用量子力学原理开发新一代计算机",
      "category": "physics",
      "tier": 3,
      "cost": 800,
      "research_time": 180.0,
      "prerequisites": ["advanced_computing", "quantum_physics"],
      "unlocks": [
        {"type": "component", "id": "quantum_core"},
        {"type": "building", "id": "quantum_lab"},
        {"type": "bonus", "target": "research_speed", "value": 0.15}
      ]
    },
    {
      "id": "plasma_weapons",
      "name": "等离子武器",
      "description": "开发高能等离子体武器系统",
      "category": "military",
      "tier": 3,
      "cost": 600,
      "research_time": 150.0,
      "prerequisites": ["energy_weapons", "plasma_physics"],
      "unlocks": [
        {"type": "component", "id": "plasma_cannon_mk2"},
        {"type": "bonus", "target": "ship_attack", "value": 0.2}
      ]
    }
  ]
}
```

## 自定义配方 (data/recipes.json)

```json
{
  "recipes": [
    {
      "id": "craft_quantum_core",
      "name": "制造量子核心",
      "category": "components",
      "tier": 4,
      "inputs": [
        {"item": "titanium_ingot", "amount": 10},
        {"item": "quantum_crystal", "amount": 3},
        {"item": "energy", "amount": 1000},
        {"item": "rare_earth", "amount": 5}
      ],
      "output": {
        "item": "quantum_core",
        "amount": 1
      },
      "duration": 30.0,
      "facility": "quantum_lab",
      "requirements": {
        "tech": "quantum_computing",
        "level": 2
      }
    }
  ]
}
```

## 语言包 (locale/zh-CN.json)

```json
{
  "locale": "zh-CN",
  "strings": {
    "component.quantum_core.name": "量子核心",
    "component.quantum_core.desc": "利用量子计算优化所有系统",
    "component.plasma_cannon_mk2.name": "等离子炮 Mk2",
    "component.plasma_cannon_mk2.desc": "改进型等离子武器，具有更高的射速和穿透力",
    "tech.quantum_computing.name": "量子计算",
    "tech.quantum_computing.desc": "利用量子力学原理开发新一代计算机",
    "tech.plasma_weapons.name": "等离子武器",
    "tech.plasma_weapons.desc": "开发高能等离子体武器系统",
    "recipe.craft_quantum_core.name": "制造量子核心",
    "recipe.craft_quantum_core.desc": "在量子实验室中制造量子核心"
  }
}
```

## 加载测试

将 `EnhancedTechMod` 放入 `mods/` 目录，启动游戏后：

1. 模组列表中出现"增强科技扩展包"
2. 科技树中出现"量子计算"和"等离子武器"
3. 组件列表中新增"量子核心"和"等离子炮 Mk2"
4. 经济数值按 balance.json 调整
5. 中文语言包生效
