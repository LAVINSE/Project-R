using System;

using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace ProjectR.Enum
{
    #region 재화
    /// <summary>
    /// 보상으로 지급하는 재화의 종류입니다.
    /// </summary>
    public enum ERewardType
    {
        /// <summary>코인 재화입니다.</summary>
        [InspectorName("코인")] Coin,

        /// <summary>달러 재화입니다.</summary>
        [InspectorName("달러")] Dollar,

        /// <summary>다이아몬드 재화입니다.</summary>
        [InspectorName("다이아몬드")] Diamond,
    }
    #endregion // 재화

    #region 이상물체 등급
    /// <summary>
    /// 이상물체의 등급입니다.
    /// </summary>
    /// <remarks>
    /// 등급은 정산 수치를 만들지 않습니다. 수치는 정의 에셋에 직접 적습니다.
    /// 등급은 좋은 물건을 주웠다는 것을 한눈에 알아보게 하는 표시 용도입니다.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.Data", sourceAssembly: "ProjectR.Data", sourceClassName: "EAnomalyGrade")]
    public enum EAnomalyGrade
    {
        /// <summary>흔하게 나오는 등급입니다.</summary>
        Common = 0,

        /// <summary>드물게 나오는 등급입니다.</summary>
        Rare = 1,

        /// <summary>거의 나오지 않는 등급입니다.</summary>
        Special = 2,
    }
    #endregion // 이상물체 등급

    #region 방송 상태
    /// <summary>
    /// 방송이 지금 어떤 상황을 내보내고 있는지를 나타내는 지속 상태입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 6.1절의 수익 구조 표를 그대로 옮긴 것입니다.
    /// 시청자 증감과 후원 흐름이 이 상태를 보고 정해집니다.
    /// <para>
    /// 순간적으로 벌어진 일을 나타내는 <see cref="EBroadcastMoment"/>와는 다릅니다.
    /// 이쪽은 "지금 어떤 상황이 이어지고 있는가"이고, 저쪽은 "방금 무슨 일이 있었는가"입니다.
    /// 몬스터에게 쫓기는 것은 상태이고, 몬스터를 처음 본 순간은 순간입니다.
    /// </para>
    /// <para>
    /// 백룸과 방송이 함께 사용하는 열거형이므로 공통 어셈블리에 둡니다.
    /// 각 기능이 공통 정의를 참조하여 백룸과 방송 사이에 직접 의존성이 생기지 않도록 합니다.
    /// </para>
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.Activity", sourceAssembly: "ProjectR.Activity", sourceClassName: "EBroadcastState")]
    public enum EBroadcastState
    {
        /// <summary>안전한 곳에 숨어 있습니다. 시청자가 빠지고 후원이 없습니다.</summary>
        Hidden = 0,

        /// <summary>탐험 중입니다. 시청자가 유지되고 후원이 소액 들어옵니다.</summary>
        Exploring = 1,

        /// <summary>미션을 수행 중입니다. 시청자가 오릅니다.</summary>
        Mission = 2,

        /// <summary>몬스터에게 쫓기고 있습니다. 시청자가 급증하고 후원이 터집니다.</summary>
        Chased = 3,

        /// <summary>이상물체를 공개하고 있습니다. 시청자가 급증하고 후원이 대량으로 들어옵니다.</summary>
        Revealing = 4,
    }
    #endregion // 방송 상태

    #region 방송 상황
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
    [MovedFrom(true, sourceNamespace: "ProjectR.Activity", sourceAssembly: "ProjectR.Activity", sourceClassName: "EBroadcastMoment")]
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
    #endregion // 방송 상황

    #region 미로 방향
    /// <summary>
    /// 격자 미로에서 이웃 칸을 가리키는 네 방향입니다.
    /// </summary>
    /// <remarks>
    /// 벽 정보를 비트로 다루기 위해 값이 1, 2, 4, 8로 배정되어 있습니다.
    /// </remarks>
    [Flags]
    [MovedFrom(true, sourceNamespace: "ProjectR.Backrooms.Generation", sourceAssembly: "ProjectR.Backrooms", sourceClassName: "EMazeDirection")]
    public enum EMazeDirection
    {
        /// <summary>방향 없음입니다.</summary>
        None = 0,
        /// <summary>위쪽(+Y) 방향입니다.</summary>
        North = 1,
        /// <summary>오른쪽(+X) 방향입니다.</summary>
        East = 2,
        /// <summary>아래쪽(-Y) 방향입니다.</summary>
        South = 4,
        /// <summary>왼쪽(-X) 방향입니다.</summary>
        West = 8,
        /// <summary>네 방향 모두입니다.</summary>
        All = North | East | South | West,
    }
    #endregion // 미로 방향

    #region 몬스터 행동
    /// <summary>
    /// 몬스터가 지금 무엇을 하고 있는지 나타내는 행동 모드입니다.
    /// </summary>
    /// <remarks>
    /// 플레이어는 몬스터의 내부 판단을 볼 수 없으므로, 모드가 바뀌는 순간마다 소리를 냅니다.
    /// 소리가 없으면 무슨 일이 일어났는지 알 수 없고, 그러면 대처가 실력이 될 수 없습니다.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.Backrooms.Monster", sourceAssembly: "ProjectR.Backrooms", sourceClassName: "EMonsterMode")]
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
    #endregion // 몬스터 행동

    #region 바닥 재질
    /// <summary>
    /// 발소리를 다르게 낼 바닥 재질입니다.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "ProjectR.Backrooms.Audio", sourceAssembly: "ProjectR.Backrooms", sourceClassName: "EFootstepSurface")]
    public enum EFootstepSurface
    {
        /// <summary>기본값입니다. 딱딱하고 울림이 짧습니다.</summary>
        Concrete = 0,
        /// <summary>타일입니다. 밝고 울림이 깁니다.</summary>
        Tile = 1,
        /// <summary>카펫입니다. 둔하고 짧습니다.</summary>
        Carpet = 2,
    }
    #endregion // 바닥 재질
}
