#!/usr/bin/env python3
"""Generate PCEdit.App.Core/Data/LogisticsGroups.json.

A logistics container's ``demandGrps`` / ``supplyGrps`` is a comma-separated list of
*resource group* ids. These are their own id space (they overlap with, but are not,
``WorldObject.GId``), so the Inventories page needs a dedicated pick-list. The ids below
were seeded from every ``demandGrps`` / ``supplyGrps`` value across the sample saves;
friendly names come from the item catalog where an id matches, otherwise from a small
set of heuristics. Curate the OVERRIDES table, then re-run.

The list is not exhaustive (a newer game build can add ids) so the UI still lets a
user type an id that is not here.
"""
import collections
import json
import re

# Seeded from demandGrps / supplyGrps across the sample saves. Keep sorted.
GROUP_IDS = [
    "Algae1Seed", "Alloy", "Aluminium", "AnimalFood1", "AnimalFood2", "AnimalFood3",
    "AntiToxinsExplosive1", "AntiToxinsExplosive2", "Bacteria1", "BalzarQuartz", "Bauxite",
    "Bee1Larvae", "Bioplastic1", "Butterfly1Larvae", "Butterfly2Larvae", "Butterfly3Larvae",
    "Butterfly4Larvae", "Butterfly5Larvae", "Butterfly6Larvae", "Butterfly7Larvae",
    "Butterfly8Larvae", "Butterfly9Larvae", "Butterfly10Larvae", "Butterfly11Larvae",
    "Butterfly12Larvae", "Butterfly13Larvae", "Butterfly14Larvae", "Butterfly15Larvae",
    "Butterfly16Larvae", "Butterfly17Larvae", "Butterfly18Larvae", "Butterfly19Larvae",
    "Butterfly20Larvae", "ChlorineCapsule1", "CircuitBoard1", "Cobalt", "CookCake1",
    "CookChocolate", "CookCocoaGrowable", "CookCookie1", "CookCroissant", "CookFlour",
    "CookStew1", "CookStewFish1", "CookWheatGrowable", "CosmicQuartz", "DNASequence",
    "Dolomite", "Drone1", "Drone2", "Explosive", "FabricBlue", "Fertilizer1", "Fertilizer2",
    "Fertilizer3", "Fish1Eggs", "Fish2Eggs", "Fish3Eggs", "Fish4Eggs", "Fish5Eggs",
    "Fish6Eggs", "Fish7Eggs", "Fish8Eggs", "Fish9Eggs", "Fish10Eggs", "Fish11Eggs",
    "Fish12Eggs", "Fish13Eggs", "Fish14Eggs", "Fish15Eggs", "Flare", "Frog1Eggs",
    "Frog2Eggs", "Frog3Eggs", "Frog4Eggs", "Frog5Eggs", "Frog6Eggs", "Frog7Eggs",
    "Frog8Eggs", "Frog9Eggs", "Frog10Eggs", "Frog11Eggs", "Frog12Eggs", "Frog13Eggs",
    "Frog14Eggs", "Frog15Eggs", "Frog16Eggs", "FrogGoldEggs", "FuseAnimals1", "FuseCartridge",
    "FuseEnergy1", "FuseGrowth1", "FuseHeat1", "FuseInsects1", "FuseOxygen1", "FusePlants1",
    "FusePressure1", "FuseProduction1", "FusePurification1", "FuseTradeRocketsSpeed1",
    "FusionEnergyCell", "GeneticTrait", "Iridium", "Iron", "Keycard1", "KeyCard2",
    "LarvaeBase1", "LarvaeBase2", "LarvaeBase3", "Magnesium", "MagnetarQuartz",
    "MethanCapsule1", "MicroPlastics", "Minable-Tungsten", "Mutagen1", "Mutagen2", "Mutagen3",
    "Mutagen4", "NitrogenCapsule1", "Obsidian", "Osmium", "OxygenCapsule1", "Phosphorus",
    "Phytoplankton1", "Phytoplankton2", "Phytoplankton3", "PlasticPolymer", "PristineMushroom",
    "PulsarQuartz", "PurificationCapsule", "PurificationGel", "PurifiedWater", "QuasarQuartz",
    "RedPowder1", "RocketReactor", "RocketReactor2", "Rod-alloy", "Rod-iridium", "Rod-osmium",
    "Rod-plastic", "Rod-tungsten", "Rod-uranium", "Seed0", "Seed1", "Seed2", "Seed3", "Seed4",
    "Seed5", "Seed6", "Seed7Humble", "Seed8Humble", "Seed9Humble", "Seed10Humble",
    "Seed11Humble", "SeedGold", "Selenium", "Silicon", "Silk", "SilkWorm", "SmartFabric",
    "SolarQuartz", "Sulfur", "Titanium", "ToxicGoo", "ToxicSpores", "ToxicWater",
    "ToxicityAmmo", "ToxicityAmmoPack", "ToxicityMedecine", "ToxicityMedecinePack", "Toxins",
    "Tree0Seed", "Tree1Seed", "Tree2Seed", "Tree3Seed", "Tree4Seed", "Tree5Seed", "Tree6Seed",
    "Tree7Seed", "Tree8Seed", "Tree9Seed", "Tree10Seed", "Tree11Seed", "Tree12Seed",
    "Tree13Seed", "Tree14Seed", "Tree15Seed", "Tree16Seed", "Tree17Seed", "TreeRoot",
    "Uraninite", "Uranim", "Vegetable0Growable", "Vegetable0Seed", "Vegetable1Growable",
    "Vegetable1Seed", "Vegetable2Growable", "Vegetable2Seed", "Vegetable3Growable",
    "Vegetable3Seed", "WaterBottle1", "Zeolite", "astrofood", "astrofood2", "honey", "ice",
]

# Hand-written names that the heuristics can't get right.
OVERRIDES = {
    "Uranim": "Uranium",
    "Uraninite": "Uraninite Ore",
    "Minable-Tungsten": "Tungsten Ore",
    "Bauxite": "Bauxite Ore",
    "Dolomite": "Dolomite Ore",
    "Selenium": "Selenium Ore",
    "Phosphorus": "Phosphorus Ore",
    "FabricBlue": "Fabric",
    "RedPowder1": "Explosive Powder",
    "Bioplastic1": "Bioplastic Nugget",
    "CircuitBoard1": "Circuit Board",
    "Bacteria1": "Bacteria Sample",
    "DNASequence": "DNA Sequence",
    "GeneticTrait": "Genetic Trait",
    "SmartFabric": "Smart Fabric",
    "MicroPlastics": "Microplastics",
    "PlasticPolymer": "Plastic Polymer",
    "SilkWorm": "Silkworm",
    "astrofood": "Space Food",
    "astrofood2": "Space Food T2",
    "honey": "Honey",
    "ice": "Ice",
    "WaterBottle1": "Water Bottle",
    "PurifiedWater": "Purified Water",
    "PurificationGel": "Purification Gel",
    "ToxicWater": "Toxic Water",
    "ToxicGoo": "Toxic Goo",
    "ToxicSpores": "Toxic Spores",
    "Toxins": "Toxins",
    "PristineMushroom": "Pristine Mushroom",
    "FusionEnergyCell": "Fusion Energy Cell",
    "RocketReactor": "Rocket Reactor",
    "RocketReactor2": "Rocket Reactor T2",
    "TreeRoot": "Tree Root",
    "Explosive": "Explosive Charge",
    "Flare": "Flare",
    "CookFlour": "Flour",
    "CookChocolate": "Chocolate",
    "CookCocoaGrowable": "Growing Cocoa",
    "CookWheatGrowable": "Growing Wheat",
}

_QUARTZ = re.compile(r"^(Pulsar|Solar|Magnetar|Cosmic|Quasar|Balzar)Quartz$")
_ROD = re.compile(r"^Rod-(.+)$")
_CAPSULE = re.compile(r"^(\w+?)Capsule\d*$")
_FUSE = re.compile(r"^Fuse(\w+?)\d*$")
_NUMBERED = re.compile(r"^([A-Za-z]+?)(\d+)(Eggs|Larvae|Seed|Growable|Humble)?$")
_TIER = re.compile(r"^([A-Za-z]+?)(\d+)$")


def humanize(gid: str) -> str:
    if gid in OVERRIDES:
        return OVERRIDES[gid]

    m = _QUARTZ.match(gid)
    if m:
        return f"{m.group(1)} Quartz"

    m = _ROD.match(gid)
    if m:
        return f"{m.group(1).capitalize()} Rod"

    m = _NUMBERED.match(gid)
    if m and m.group(3):
        base, num, kind = m.group(1), m.group(2), m.group(3)
        noun = {
            "Eggs": "Eggs", "Larvae": "Larva", "Seed": "Seed",
            "Growable": "(growing)", "Humble": "Seed",
        }[kind]
        base = re.sub(r"(?<!^)(?=[A-Z])", " ", base)
        if kind == "Humble":
            return f"Humble Seed ({num})"
        if kind == "Growable":
            return f"Growing {base} ({num})"
        return f"{base} {noun} ({num})"

    m = _FUSE.match(gid)
    if m:
        return f"{m.group(1)} Fuse"

    m = _CAPSULE.match(gid)
    if m:
        return f"{m.group(1)} Capsule"

    # CamelCase / trailing tier number -> "Camel Case T2"
    m = _TIER.match(gid)
    if m:
        spaced = re.sub(r"(?<!^)(?=[A-Z])", " ", m.group(1))
        return f"{spaced} T{m.group(2)}"

    return re.sub(r"(?<!^)(?=[A-Z])", " ", gid)


def main():
    groups = collections.OrderedDict()
    for gid in sorted(GROUP_IDS, key=str.lower):
        groups[gid] = humanize(gid)

    out = collections.OrderedDict()
    out["$comment"] = (
        "App-only pick-list for a logistics container's demand / supply groups "
        "(Inventory.demandGrps / supplyGrps). Not part of the save file, and not "
        "exhaustive - the UI still accepts an id typed by hand. Regenerate with "
        "tools/item-catalog/gen_logistics_groups.py."
    )
    out["groups"] = groups

    path = "PCEdit.App.Core/Data/LogisticsGroups.json"
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(json.dumps(out, indent=2) + "\n")
    print(f"wrote {path}: {len(groups)} groups")


if __name__ == "__main__":
    main()
