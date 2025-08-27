using System;
using System.Collections.Generic;
using Godot;

namespace meph {
    [GlobalClass]
    public partial class CharacterData : CardData {
        [Export] public string charName;
        [Export] public Character.STAR star;
        [Export] public Character.ESSENCE_TYPE essenceType;
        [Export] public Character.WEAPON_TYPE weaponType;

        [Export] public int maxLP;
        [Export] public int maxEP;
        [Export] public int maxMP;

        // Unused, might be useful later
        [Export] public Godot.Collections.Array<CardData> cards = [];

        [Export] public int DEF = 50;
        [Export] public int essenceDEF = 50;

        [Export] public float critRate = 0.10f;
        [Export] public float critDamage = 0.05f;
    }

    // [GlobalClass]
    // public partial class SetBonusData : Resource {
    //     [Export] public string setName;
    //     [Export] public string description;
    // }
}