using UnityEngine.Scripting.APIUpdating;

namespace ProjectR.UI.Popup
{
    /// <summary>
    /// 팝업 카탈로그에 등록한 키 모음입니다.
    /// </summary>
    /// <remarks>
    /// 팝업을 키로 부르면 부르는 쪽이 프리팹 참조를 들고 있지 않아도 됩니다.
    /// 그래서 씬마다 같은 프리팹을 이어 두던 자리가 사라지고, 프리팹을 바꿔도 카탈로그만 고치면 됩니다.
    /// 키를 문자열로 직접 적으면 오타가 실행할 때까지 드러나지 않으므로 여기 상수로 모아 둡니다.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.UI", sourceAssembly: "ProjectR.UI", sourceClassName: "PopupKeys")]
    public static class PopupKeys
    {
        #region 상수
        /// <summary>ESC로 여는 옵션 팝업입니다.</summary>
        public const string Options = "options";

        /// <summary>탐험이 끝난 뒤 관리 화면에서 뜨는 정산 팝업입니다.</summary>
        public const string Settlement = "settlement";
        #endregion // 상수
    }
}
