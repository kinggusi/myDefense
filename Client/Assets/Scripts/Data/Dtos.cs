using System;

[Serializable]
public class GameResponseDto {
    public string message;
    public InGameAlien alien;
    public int remainingGold;
    public bool isGameOver;
}

[Serializable]
public class InGameAlien {
    public long id;
    public AlienSpec alienSpec;
    public string pendingMutationType;
    public string activeMutationType;
    public int mutationRerollCount;
    public int gridX;
    public int gridY;
}

[Serializable]
public class AlienSpec {
    public long id;
    public string name;
    public string description;
    public int baseAtk;
    public int baseMp;
    public float atkSpeed;
    public float range;
    public long evolutionTargetId;
    public string grade; // NORMAL, EPIC, UNIQUE
    public bool locked;
}

[Serializable]
public class MergeRequestDto {
    public long userId;
    public long sourceId;
    public long targetId;
}
