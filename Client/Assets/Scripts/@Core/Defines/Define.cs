using UnityEngine;

public class Define
{
    public enum Scene {Unknown, Lobby, Game}
    public struct Tags
    {
        public const string Player = "Player";
        public const string Monster = "Monster";
    }

    // 왹져 생체 변이(접두사) 목록
        public enum MutationPrefix { None, Obese, Unstable, Frozen }
}
