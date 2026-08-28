using UnityEngine;

namespace ProjectR.Backrooms
{
    /// <summary>
    /// 백룸 안에서 소리가 났음을 알리는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// 누가 들을지는 소리를 낸 쪽이 정하지 않습니다. 소리는 위치와 들리는 반경만 알리고,
    /// 그 반경 안에 있는지는 듣는 쪽이 스스로 판정합니다.
    /// 이렇게 두어야 몬스터가 늘어나도 소리를 내는 쪽은 그대로 둘 수 있습니다.
    /// </remarks>
    public readonly struct NoiseEmittedEvent
    {
        #region 프로퍼티
        /// <summary>소리가 난 위치입니다.</summary>
        public Vector3 Position { get; }

        /// <summary>소리가 들리는 최대 반경(미터)입니다.</summary>
        public float Radius { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 소리가 난 위치와 들리는 반경을 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="position">소리가 난 위치입니다.</param>
        /// <param name="radius">소리가 들리는 최대 반경(미터)입니다.</param>
        public NoiseEmittedEvent(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
        #endregion // 함수
    }
}
