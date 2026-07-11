using System;
using UnityEngine;

[Serializable]
public class GameResponseDto {
    public string message;
    public InGameAlien alien; // 레거시 호환성을 위해 InGameAlien 유지
    public int remainingGold;
    public bool isGameOver;
}

[Serializable]
public class GameResponseObjectDto {
    public string message;
    public BoardObjectDto alien; // 다형성 수용이 필요한 새 응답을 위한 DTO
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
public class ApiErrorResponse
{
    public string code;
    public string message;
}

[Serializable]
public class BoardObjectDto
{
    public const string TypeAlien = "ALIEN";
    public const string TypeInjector = "MUTATION_INJECTOR";

    public long id;
    public string objectType; // ALIEN 또는 MUTATION_INJECTOR
    public int gridX;
    public int gridY;

    // Alien 전용 속성들
    public AlienSpec alienSpec;
    public string pendingMutationType;
    public string activeMutationType;
    public int mutationRerollCount;

    // Injector 전용 속성들
    public string mutationType;
}

public class ApiResult<T>
{
    public bool IsSuccess;
    public long StatusCode;
    public T Data;
    public ApiErrorResponse Error;
    public string NetworkError;
}

[Serializable]
public class EmptyRequestBody {}

public enum BoardObjectKind
{
    Alien,
    Injector
}

public static class BoardObjectHelper
{
    public static bool TryGetBoardObject(GameObject obj, out long serverId, out BoardObjectKind kind)
    {
        serverId = -1;
        kind = BoardObjectKind.Alien;

        if (obj == null) return false;

        // 1. Alien 검사 (UnitData의 grade가 INJECTOR가 아닌 정상 Alien 유닛만 필터링)
        UnitData alienData = obj.GetComponent<UnitData>();
        if (alienData != null && alienData.grade != "INJECTOR")
        {
            serverId = alienData.serverId;
            kind = BoardObjectKind.Alien;
            return true;
        }

        // 2. Injector 검사
        InjectorData injectorData = obj.GetComponent<InjectorData>();
        if (injectorData != null)
        {
            serverId = injectorData.serverId;
            kind = BoardObjectKind.Injector;
            return true;
        }

        return false;
    }
}
