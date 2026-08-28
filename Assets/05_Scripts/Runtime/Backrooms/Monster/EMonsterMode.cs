namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터가 지금 무엇을 하고 있는지 나타내는 행동 모드입니다.
    /// </summary>
    /// <remarks>
    /// 플레이어는 몬스터의 내부 판단을 볼 수 없으므로, 모드가 바뀌는 순간마다 소리를 냅니다.
    /// 소리가 없으면 무슨 일이 일어났는지 알 수 없고, 그러면 대처가 실력이 될 수 없습니다.
    /// </remarks>
    public enum EMonsterMode
    {
        /// <summary>아직 아무 행동도 시작하지 않은 상태입니다.</summary>
        None = 0,

        /// <summary>목적 없이 통로를 돌아다니는 중입니다.</summary>
        Patrol,

        /// <summary>플레이어를 보고 쫓는 중입니다.</summary>
        Chase,

        /// <summary>놓친 자리 주변을 뒤지는 중입니다.</summary>
        Search,

        /// <summary>탈출 지점 근처에서 기다리는 중입니다.</summary>
        Ambush,

        /// <summary>배회하던 자리로 돌아가는 중입니다.</summary>
        Return,
    }
}
