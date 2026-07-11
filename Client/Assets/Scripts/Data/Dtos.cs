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

[Serializable]
public class LobbyResponseDto {
    public UserDto user;
    public System.Collections.Generic.List<AlienInventoryDto> aliens;
}

[Serializable]
public class UserDto {
    public string username;
    public int gold;
    public int diamond;
    public int heart;
}

[Serializable]
public class AlienInventoryDto {
    public long id;
    public string name;
    public string grade;
    public int level;
    public int pieces;
    public int requiredPieces;
    public bool locked;
}
using System;
using System.Collections.Generic;

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
