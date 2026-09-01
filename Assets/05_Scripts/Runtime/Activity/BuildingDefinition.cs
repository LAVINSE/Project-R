using System;

using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 마을에 서 있는 건물 하나의 정의 에셋입니다.
    /// </summary>
    /// <remarks>
    /// 체크리스트 2.6절입니다. <b>건물을 늘리는 것이 활동을 늘리는 것과 같은 일이 되게 합니다.</b>
    /// <see cref="AnomalyDefinition"/>과 같은 방식으로 <see cref="SWIdentifiedObject"/>를 상속하고
    /// <see cref="SWIODatabase"/>에 모읍니다.
    /// <para>
    /// <b>마을 좌표와 모델은 여기 없습니다. 씬에 둡니다.</b> 체크리스트 2.6절이 M1에서 실제로 배치해 보고
    /// 정하라고 남겨 둔 결정이고, 배치해 보니 씬 쪽이 나았습니다(진행기록 17.1절).
    /// 마을이 3D가 되면서 이 판단이 더 굳어졌습니다. 건물 하나가 모델 조각 여럿으로 이루어지므로
    /// 정의가 프리팹 하나를 가리키는 방식으로는 담기지 않습니다(진행기록 18.1절).
    /// 정의가 아는 것은 <b>무엇을 하는 건물인가</b>뿐입니다.
    /// </para>
    /// <para>
    /// 방송 시간 비용도 여기 없습니다. <see cref="IActivityFactory.BroadcastCost"/>가 들고 있습니다.
    /// 두 곳에 적으면 활동의 비용을 바꿨을 때 건물에 적힌 안내가 조용히 어긋납니다.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "Building", menuName = "프로젝트R/건물 정의")]
    public class BuildingDefinition : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("겉모습")]
        [SerializeField, Tooltip("화면에 이 건물을 작게 나타낼 아이콘입니다. 마을의 모델은 씬에 있습니다.")]
        private Sprite icon;

        [SWGroup("활동")]
        [SerializeReference, SWSubClassSelector, Tooltip("이 건물에 들어갔을 때 시작할 활동을 만드는 방법입니다. 비우면 활동이 아닙니다.")]
        private IActivityFactory activityFactory;

        [SerializeField, Tooltip("활동이 아니라 방 화면으로 들어가는 건물이면 켭니다. 집이 그렇습니다.")]
        private bool opensRoom;

        [SerializeField, Tooltip("건물 아래에 적을 한 줄입니다. 이 건물이 무엇을 바꾸는지를 적습니다.")]
        private string effectNotice;

        [SWGroup("해금")]
        [SerializeField, Min(1), Tooltip("이 날짜부터 들어갈 수 있습니다. 1이면 처음부터 열려 있습니다.")]
        private int requiredDay = 1;

        [SerializeField, Tooltip("잠겼을 때 알려 줄 이유입니다. 비우면 날짜로 만든 문구를 씁니다.")]
        private string lockedNotice;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 건물을 가리킬 때 쓰는 식별자입니다.</summary>
        /// <remarks>코드명을 비워 두면 에셋 이름을 대신 씁니다.</remarks>
        public string DefinitionId => string.IsNullOrEmpty(CodeName) ? name : CodeName;

        /// <summary>화면에 이 건물을 작게 나타낼 아이콘입니다. 없으면 null입니다.</summary>
        public Sprite Icon => icon;

        /// <summary>이 건물이 활동으로 이어지는지 여부입니다.</summary>
        public bool HasActivity => activityFactory != null;

        /// <summary>이 건물이 활동이 아니라 방 화면으로 들어가는지 여부입니다.</summary>
        /// <remarks>
        /// 활동이 없다는 것만으로 방으로 보내면 안 됩니다. <b>아직 활동을 만들지 않은 건물</b>과
        /// <b>원래 활동이 아닌 건물</b>은 다릅니다. 앞의 것을 뒤의 것으로 취급하면
        /// 아르바이트를 눌렀는데 방이 열립니다. 그래서 명시적으로 갈라 둡니다.
        /// </remarks>
        public bool OpensRoom => opensRoom;

        /// <summary>이 건물에 들어갈 때 드는 방송 시간(분)입니다. 활동이 아니면 0입니다.</summary>
        public int BroadcastCost => activityFactory?.BroadcastCost ?? 0;

        /// <summary>건물 아래에 적을 한 줄입니다.</summary>
        public string EffectNotice => effectNotice ?? string.Empty;

        /// <summary>이 날짜부터 들어갈 수 있습니다.</summary>
        public int RequiredDay => requiredDay;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 이 건물의 활동을 만듭니다.
        /// </summary>
        /// <returns>만들어진 활동입니다. 활동이 아닌 건물이면 null을 반환합니다.</returns>
        public IActivity CreateActivity()
        {
            return activityFactory?.Create();
        }

        /// <summary>
        /// 지금 상태에서 이 건물이 해금되었는지 확인합니다.
        /// </summary>
        /// <param name="state">확인에 쓸 게임 상태입니다.</param>
        /// <returns>해금되었으면 true를 반환합니다.</returns>
        public bool IsUnlocked(GameState state)
        {
            return state != null && state.Day >= requiredDay;
        }

        /// <summary>
        /// 지금 이 건물에 들어갈 수 없는 이유를 돌려줍니다.
        /// </summary>
        /// <param name="state">확인에 쓸 게임 상태입니다.</param>
        /// <returns>들어갈 수 없는 이유입니다. 들어갈 수 있으면 빈 문자열을 반환합니다.</returns>
        /// <remarks>
        /// 버튼을 끄기만 하면 왜 안 되는지 알 수 없습니다(체크리스트 2.2절).
        /// 그래서 막는 쪽이 이유까지 함께 만들어 냅니다.
        /// </remarks>
        public string GetBlockedReason(GameState state)
        {
            if (state == null) return "상태를 읽지 못했습니다";

            if (IsUnlocked(state) == false)
            {
                return string.IsNullOrEmpty(lockedNotice) ? $"{requiredDay}일차부터 열립니다" : lockedNotice;
            }

            // 활동도 없고 방으로 가는 것도 아니면 아직 만들지 않은 건물입니다.
            // 조용히 아무 일도 없는 것보다 만들지 않았다고 말하는 편이 낫습니다.
            if (HasActivity == false && OpensRoom == false) return "아직 만들지 않았습니다";

            if (HasActivity && BroadcastCost > state.RemainingBroadcastMinutes)
            {
                return $"방송 시간이 {BroadcastCost - state.RemainingBroadcastMinutes}분 모자랍니다";
            }

            return string.Empty;
        }
        #endregion // 함수
    }
}
