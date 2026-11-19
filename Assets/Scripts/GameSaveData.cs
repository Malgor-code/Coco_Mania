using System.Collections.Generic;
[System.Serializable]
public class GameSaveData
{
    public int version = 1;
    public int legacyPoints;
    public int totalLegacyPointsEarned;
    public int coins;
    public int bestRunKills;
    public int bestCycleReached = 1;
    public List<string> purchasedLegacyItems = new List<string>();
    public bool hasRun;
    public int runPhase;         
    public bool forcedDecision;
    public bool skipNextDayDecrement;
    public int runDayCount;
    public int billCycle = 1;
    public int daysLeft;
    public bool victoryAwarded;
    public float water;
    public int equippedMachete;
    public int runCoconutsKilled;
    public int runMoneyEarned;
    public int totalCoconutsKilled;
    public int pendingPerkOffers;
    public List<string> unlockedSkills = new List<string>();
    public List<string> chosenPerks = new List<string>(); 
    public float[] relicStats;                       
}