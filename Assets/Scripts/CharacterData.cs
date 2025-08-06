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
        [Export] public int maxUP;
        [Export] public int maxPotion;

        [Export] public Godot.Collections.Array<CardData> cards = new ( );
        [Export] public Godot.Collections.Array<CharmData> charms = new ( );
        [Export] public SetBonusData setBonus;
    }

    // Moved to it's own resource, keeping it for clarity

    // [GlobalClass]
    // public partial class CardData : Resource {
    //     [Export] public string id;
    //     [Export] public string name;
    //     [Export] public string type;
    //     [Export] public string description;
    //     [Export] public Godot.Collections.Dictionary<string, int> requirements = new ( );
    // }

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