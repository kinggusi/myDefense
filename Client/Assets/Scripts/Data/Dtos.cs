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
    public int gridX;
    public int gridY;
}

[Serializable]
public class AlienSpec {
    public long id;
    public string name;
    public string grade; // NORMAL, EPIC, UNIQUE
}

[Serializable]
public class MergeRequestDto {
    public long userId;
    public long sourceId;
    public long targetId;
}