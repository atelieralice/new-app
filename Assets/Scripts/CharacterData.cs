using System;
using System.Collections.Generic;

// This file is for getting character data from a json and handling it in a way that can be used by the game
namespace meph {

    [Serializable]
    public class CharacterData {
        public string charName;
        public int star;
        public string essenceType;
        public string weaponType;

        public int maxLP;
        public int maxEP;
        public int maxMP;
        public int maxUP;
        public int maxPotion;

        public List<CardData> cards; // = new List<CardData>(); -> NOT NEEDED!
        public List<CharmData> charms;
        public SetBonusData setBonus;
    }

    [Serializable]
    public class CardData {
        public string id;
        public string name;
        public string type;
        public string description;
        public Dictionary<string, int> requirements;
    }

    [Serializable]
    public class CharmData {
        public string id;
        public string name;
        public string description;
    }

    [Serializable]
    public class SetBonusData {
        public string setName;
        public string description;
    }
}