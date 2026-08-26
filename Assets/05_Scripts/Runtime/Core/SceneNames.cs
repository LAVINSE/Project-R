namespace ProjectR.Core
{
    /// <summary>
    /// 프로젝트에서 사용하는 씬 이름 상수를 모아 둔 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 씬 이름을 문자열로 직접 쓰면 오타를 컴파일 시점에 잡을 수 없으므로 반드시 이 상수를 사용합니다.
    /// </remarks>
    public static class SceneNames
    {
        #region 상수
        /// <summary>관리 화면 씬의 이름입니다.</summary>
        public const string Home = "HomeScene";

        /// <summary>씬 전환 중 표시되는 로딩 씬의 이름입니다.</summary>
        public const string Loading = "LoadingScene";

        /// <summary>백룸 탐험 씬의 이름입니다.</summary>
        public const string Backrooms = "BackroomsScene";
        #endregion // 상수
    }
}
