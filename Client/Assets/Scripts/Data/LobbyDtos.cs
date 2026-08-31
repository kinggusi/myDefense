using System;
using System.Collections.Generic;

[Serializable]
public class LobbyResponseDto 
{
    public UserDto user;
    public List<AlienInventoryDto> aliens;
}

[Serializable]
public class UserDto 
{
    public string username;
    public int gold;
    public int diamond;
    public int heart;
    public int accountLevel;
    public int universalPiece;
    public int growthCell;
}

[Serializable]
public class AlienInventoryDto 
{
    public long id;
    public string name;
    public string grade;
    public int level;
    public int pieces;
    public int requiredPieces;
    public bool locked;
    public bool owned;
    public bool specLocked;
}
