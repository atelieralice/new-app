using System;
using System.Collections.Generic;
using Godot;

namespace meph {
    [GlobalClass]
    public partial class CharacterData : Resource {
        [Export] public string charName;
        [Export] public Character.STAR star;
        [Export] public Character.ESSENCE_TYPE essenceType;
        [Export] public Character.WEAPON_TYPE weaponType;

        [Export] public int maxLP;
        [Export] public int maxEP;
        [Export] public int maxMP;
        [Export] public int maxPotion;

        // Unused, might be useful later
        [Export] public Godot.Collections.Array<CardData> cards = [];
        [Export] public Godot.Collections.Array<CharmData> charms = [];

        [Export] public SetBonusData setBonus;

        [Export] public int DEF;
        [Export] public int EssenceDEF;

        [Export] public float CritRate;
        [Export] public float CritDamage;
    }

    [GlobalClass]
    public partial class CharmData : Resource {
        [Export] public string id;
        [Export] public string name;
        [Export] public string description;
    }

    [GlobalClass]
    public partial class SetBonusData : Resource {
        [Export] public string setName;
        [Export] public string description;
    }
}