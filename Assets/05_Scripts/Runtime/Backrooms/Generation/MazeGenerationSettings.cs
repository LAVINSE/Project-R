using System;

using UnityEngine;

using SW.Attributes;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 미로 한 판을 생성할 때 쓰는 설정값 묶음입니다.
    /// </summary>
    /// <remarks>
    /// 인스펙터에서 조정할 수 있도록 직렬화 가능한 클래스로 두었습니다.
    /// 층마다 다른 설정을 주는 층 정의 에셋은 프로토타입 이후 단계에서 추가합니다.
    /// </remarks>
    [Serializable]
    public class MazeGenerationSettings
    {
        #region 필드
        [SWGroup("격자 크기")]
        [SerializeField, Range(3, 64), Tooltip("미로의 가로 칸 수입니다.")]
        private int width = 16;

        [SerializeField, Range(3, 64), Tooltip("미로의 세로 칸 수입니다.")]
        private int height = 16;

        [SWGroup("생성 보정")]
        [SerializeField, Min(0), Tooltip("생성 결과가 반드시 가져야 할 최소 순환로 개수입니다. 벽 짚기 공략을 막습니다.")]
        private int minimumLoopCount = 8;

        [SerializeField, Range(0f, 1f), Tooltip("전체 칸 대비 허용하는 막다른 길의 최대 비율입니다.")]
        private float maximumDeadEndRatio = 0.1f;

        [SWGroup("넓은 홀")]
        [SerializeField, Min(0), Tooltip("트여 있는 홀을 몇 개 만들지입니다. 0이면 전부 한 칸 폭 복도가 됩니다.")]
        private int roomCount = 5;

        [SerializeField, Range(2, 8), Tooltip("홀 한 변의 최소 칸 수입니다.")]
        private int minimumRoomSize = 3;

        [SerializeField, Range(2, 8), Tooltip("홀 한 변의 최대 칸 수입니다.")]
        private int maximumRoomSize = 5;

        [SerializeField, Min(0), Tooltip("가장 넓은 트인 구역이 반드시 가져야 할 칸 수입니다. 최악 구간 측정 대상이 됩니다.")]
        private int minimumLargestOpenAreaCellCount = 9;

        [SWGroup("검증")]
        [SerializeField, Range(1, 20), Tooltip("검증에 실패했을 때 다시 생성해 볼 최대 횟수입니다.")]
        private int maximumAttemptCount = 5;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>미로의 가로 칸 수입니다.</summary>
        public int Width => width;

        /// <summary>미로의 세로 칸 수입니다.</summary>
        public int Height => height;

        /// <summary>생성 결과가 반드시 가져야 할 최소 순환로 개수입니다.</summary>
        public int MinimumLoopCount => minimumLoopCount;

        /// <summary>전체 칸 대비 허용하는 막다른 길의 최대 비율입니다.</summary>
        public float MaximumDeadEndRatio => maximumDeadEndRatio;

        /// <summary>검증 실패 시 다시 생성해 볼 최대 횟수입니다.</summary>
        public int MaximumAttemptCount => maximumAttemptCount;

        /// <summary>트여 있는 홀을 몇 개 만들지입니다.</summary>
        public int RoomCount => roomCount;

        /// <summary>홀 한 변의 최소 칸 수입니다.</summary>
        public int MinimumRoomSize => minimumRoomSize;

        /// <summary>홀 한 변의 최대 칸 수입니다. 최소 칸 수보다 작으면 최소 칸 수로 맞춥니다.</summary>
        public int MaximumRoomSize => Mathf.Max(minimumRoomSize, maximumRoomSize);

        /// <summary>가장 넓은 트인 구역이 반드시 가져야 할 칸 수입니다.</summary>
        public int MinimumLargestOpenAreaCellCount => minimumLargestOpenAreaCellCount;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 설정값을 지정해 만듭니다. 단위 테스트에서 사용합니다.
        /// </summary>
        /// <param name="width">미로의 가로 칸 수입니다.</param>
        /// <param name="height">미로의 세로 칸 수입니다.</param>
        /// <param name="minimumLoopCount">최소 순환로 개수입니다.</param>
        /// <param name="maximumDeadEndRatio">막다른 길 비율 상한입니다.</param>
        /// <param name="maximumAttemptCount">최대 재생성 횟수입니다.</param>
        /// <param name="roomCount">트여 있는 홀의 개수입니다.</param>
        /// <param name="minimumRoomSize">홀 한 변의 최소 칸 수입니다.</param>
        /// <param name="maximumRoomSize">홀 한 변의 최대 칸 수입니다.</param>
        /// <param name="minimumLargestOpenAreaCellCount">가장 넓은 트인 구역의 최소 칸 수입니다.</param>
        public MazeGenerationSettings(int width, int height, int minimumLoopCount,
            float maximumDeadEndRatio, int maximumAttemptCount,
            int roomCount, int minimumRoomSize, int maximumRoomSize, int minimumLargestOpenAreaCellCount)
        {
            this.width = width;
            this.height = height;
            this.minimumLoopCount = minimumLoopCount;
            this.maximumDeadEndRatio = maximumDeadEndRatio;
            this.maximumAttemptCount = maximumAttemptCount;
            this.roomCount = roomCount;
            this.minimumRoomSize = minimumRoomSize;
            this.maximumRoomSize = maximumRoomSize;
            this.minimumLargestOpenAreaCellCount = minimumLargestOpenAreaCellCount;
        }

        /// <summary>
        /// 기본값으로 설정을 만듭니다.
        /// </summary>
        public MazeGenerationSettings()
        {
        }
        #endregion // 함수
    }
}
