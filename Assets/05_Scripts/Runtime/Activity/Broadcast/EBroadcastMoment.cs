namespace ProjectR.Activity
{
    /// <summary>
    /// 방송 도중 벌어진 일을 나타내는 상황 태그입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 6.3절의 상황 태그 여덟 가지입니다. 채팅이 이 태그를 보고 문장을 고릅니다.
    /// 태그와 템플릿을 조합하는 방식을 쓰는 이유는 안정적이고 비용이 없으며
    /// 오프라인에서도 도는 데다, 문장 생성 방식에 붙는 스팀 신고 의무를 피할 수 있기 때문입니다.
    /// <para>
    /// 채팅은 문장 품질보다 <b>흐름이 반응하는 느낌</b>이 중요합니다.
    /// 그래서 태그는 "무엇이 벌어졌나"만 나타내고 어떤 문장을 낼지는 정하지 않습니다.
    /// </para>
    /// </remarks>
    public enum EBroadcastMoment
    {
        /// <summary>놀랄 만한 일이 벌어졌습니다.</summary>
        Fear = 0,

        /// <summary>플레이어가 실수했습니다.</summary>
        Blunder = 1,

        /// <summary>무언가에 성공했습니다.</summary>
        Success = 2,

        /// <summary>한동안 아무 일도 없습니다.</summary>
        Silence = 3,

        /// <summary>연기하던 컨셉이 무너졌습니다.</summary>
        ConceptBreak = 4,

        /// <summary>이상물체를 주웠습니다.</summary>
        Collect = 5,

        /// <summary>몬스터에게 쫓기기 시작했습니다.</summary>
        Chase = 6,

        /// <summary>백룸에서 빠져나왔습니다.</summary>
        Escape = 7,
    }
}
