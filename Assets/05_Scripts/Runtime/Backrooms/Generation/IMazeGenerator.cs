using System;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 격자 미로를 만들어 내는 생성기의 공통 규격입니다.
    /// </summary>
    /// <remarks>
    /// 성능 판정 결과에 따라 생성 방식을 WFC나 BSP로 바꿀 수 있도록 인터페이스로 분리했습니다.
    /// 난수는 바깥에서 주입해 시드 재현성을 생성기 밖에서 보장합니다.
    /// </remarks>
    public interface IMazeGenerator
    {
        #region 프로퍼티
        /// <summary>로그와 통계에 표시할 생성 방식 이름입니다.</summary>
        string DisplayName { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 설정과 난수를 받아 미로를 만듭니다.
        /// </summary>
        /// <param name="settings">생성에 사용할 설정입니다.</param>
        /// <param name="random">생성에 사용할 난수 발생기입니다.</param>
        /// <returns>생성된 격자 미로입니다.</returns>
        MazeGrid Generate(MazeGenerationSettings settings, Random random);
        #endregion // 함수
    }
}
