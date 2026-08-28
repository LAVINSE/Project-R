namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터 Behaviour Tree가 쓰는 Blackboard Key 이름 모음입니다.
    /// </summary>
    /// <remarks>
    /// Key 이름을 노드마다 문자열로 적으면 오타가 컴파일에서 걸리지 않고 실행 중에야 드러납니다.
    /// 몬스터 유형이 늘어나도 트리는 하나만 두고 아래 수치 Key를 Blackboard Override로 덮어씁니다.
    /// 트리를 복제하는 순간 수치가 두 벌이 되어 어느 쪽이 진짜인지 알 수 없게 됩니다.
    /// </remarks>
    public static class MonsterBlackboardKeys
    {
        #region 상수
        /// <summary>플레이어의 현재 위치입니다. 감지 여부와 상관없이 갱신됩니다.</summary>
        public const string PlayerPosition = "PlayerPosition";

        /// <summary>지금 플레이어가 시야에 들어와 있는지 여부입니다.</summary>
        public const string CanSeePlayer = "CanSeePlayer";

        /// <summary>플레이어를 마지막으로 본 위치입니다.</summary>
        public const string LastSeenPosition = "LastSeenPosition";

        /// <summary>마지막 목격 위치를 들고 있는지 여부입니다.</summary>
        public const string HasLastSeen = "HasLastSeen";

        /// <summary>마지막으로 소리를 들은 위치입니다.</summary>
        public const string HeardPosition = "HeardPosition";

        /// <summary>아직 확인하지 않은 소리를 들고 있는지 여부입니다.</summary>
        public const string HasHeard = "HasHeard";

        /// <summary>매복하러 갈 자리입니다. 탈출 지점 근처로 정해집니다.</summary>
        public const string AmbushPosition = "AmbushPosition";

        /// <summary>매복할 자리를 알고 있는지 여부입니다.</summary>
        public const string HasAmbushPosition = "HasAmbushPosition";

        /// <summary>수색이 헛수고로 끝나 매복을 한 번 시도해 볼 수 있는 상태인지 여부입니다.</summary>
        public const string AmbushArmed = "AmbushArmed";

        /// <summary>플레이어를 알아볼 수 있는 최대 거리(미터)입니다.</summary>
        public const string SightRange = "SightRange";

        /// <summary>시야의 좌우 전체 각도(도)입니다.</summary>
        public const string SightAngle = "SightAngle";

        /// <summary>소리를 들을 수 있는 최대 거리(미터)입니다.</summary>
        public const string HearingRange = "HearingRange";

        /// <summary>배회할 때의 이동 속도(m/s)입니다.</summary>
        public const string PatrolSpeed = "PatrolSpeed";

        /// <summary>추격할 때의 이동 속도(m/s)입니다.</summary>
        public const string ChaseSpeed = "ChaseSpeed";

        /// <summary>수색할 때의 이동 속도(m/s)입니다.</summary>
        public const string SearchSpeed = "SearchSpeed";

        /// <summary>시야에서 놓친 뒤에도 계속 쫓는 시간(초)입니다.</summary>
        public const string ChaseGraceSeconds = "ChaseGraceSeconds";

        /// <summary>한 번의 수색을 이어 가는 시간(초)입니다.</summary>
        public const string SearchDurationSeconds = "SearchDurationSeconds";

        /// <summary>수색이 끝난 뒤 매복으로 넘어갈 확률입니다.</summary>
        public const string AmbushChance = "AmbushChance";

        /// <summary>매복 자리에서 기다리는 시간(초)입니다.</summary>
        public const string AmbushDurationSeconds = "AmbushDurationSeconds";

        /// <summary>플레이어를 붙잡았다고 판정하는 거리(미터)입니다.</summary>
        public const string CatchDistance = "CatchDistance";
        #endregion // 상수
    }
}
