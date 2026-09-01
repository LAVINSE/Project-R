using System;

namespace ProjectR.Broadcast
{
    /// <summary>
    /// 채팅창에 올라가는 한 줄입니다.
    /// </summary>
    /// <remarks>
    /// 닉네임과 문장을 나눠 두는 이유는 채팅창이 둘을 다른 색으로 그리기 때문입니다.
    /// 하나로 합쳐 두면 그릴 때마다 다시 잘라야 합니다.
    /// </remarks>
    [Serializable]
    public struct ChatLine
    {
        #region 필드
        /// <summary>이 줄을 쓴 시청자의 이름입니다.</summary>
        public string Nickname;

        /// <summary>이 줄의 내용입니다.</summary>
        public string Text;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>내용이 비어 있지 않은지 여부입니다.</summary>
        public bool IsValid => string.IsNullOrEmpty(Text) == false;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 닉네임과 내용을 지정해 한 줄을 만듭니다.
        /// </summary>
        /// <param name="nickname">이 줄을 쓴 시청자의 이름입니다.</param>
        /// <param name="text">이 줄의 내용입니다.</param>
        public ChatLine(string nickname, string text)
        {
            Nickname = nickname;
            Text = text;
        }
        #endregion // 함수
    }
}
