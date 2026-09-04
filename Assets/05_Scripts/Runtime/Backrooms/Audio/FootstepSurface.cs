using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectR.Enum;

namespace ProjectR.Backrooms.Audio
{
    /// <summary>
    /// 이 콜라이더를 밟았을 때 어떤 발소리를 낼지 표시해 두는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 붙어 있지 않은 바닥은 <see cref="EFootstepSurface.Concrete"/>로 봅니다.
    /// 지금은 타일 프리팹의 바닥 재질이 한 종류뿐이라 붙일 곳이 없지만,
    /// 카펫이나 타일 구역을 만들면 이 컴포넌트를 붙이는 것만으로 발소리가 갈립니다.
    /// </remarks>
    public class FootstepSurface : SWMonoBehaviour
    {
        #region 필드
        /// <summary>이 바닥을 밟았을 때 낼 발소리 재질입니다.</summary>
        [SWGroup("재질")]
        [SerializeField, Tooltip("이 바닥을 밟았을 때 낼 발소리 재질입니다.")]
        private EFootstepSurface surface = EFootstepSurface.Concrete;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 바닥의 발소리 재질입니다.</summary>
        public EFootstepSurface Surface => surface;
        #endregion // 프로퍼티
    }
}
