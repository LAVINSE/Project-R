using System;

using ProjectR.Activity;

namespace ProjectR.Broadcast
{
    /// <summary>
    /// 상황 태그와 템플릿 세트를 조합해 채팅 한 줄을 만듭니다.
    /// </summary>
    /// <remarks>
    /// 기획서 6.3절의 "상황 태그 × 템플릿" 조합이 실제로 벌어지는 자리입니다.
    /// <para>
    /// 난수 생성기를 밖에서 받는 이유는 두 가지입니다.
    /// 하나는 시험할 때 같은 씨앗으로 같은 결과가 나와야 하기 때문이고,
    /// 다른 하나는 백룸이 시드를 저장해 두는 것과 같은 이유로 재현이 필요하기 때문입니다.
    /// </para>
    /// <para>
    /// 문장이 없는 태그를 받으면 빈 줄을 돌려줍니다. 예외를 던지지 않는 이유는
    /// 아직 문장을 안 채운 태그 하나 때문에 방송이 멈추는 것이 더 나쁘기 때문입니다.
    /// 채팅은 없어도 게임이 도는 요소입니다.
    /// </para>
    /// </remarks>
    public static class ChatLineFactory
    {
        #region 함수
        /// <summary>
        /// 상황 태그에 맞는 채팅 한 줄을 만듭니다.
        /// </summary>
        /// <param name="templateSet">문장과 닉네임을 가져올 템플릿 세트입니다.</param>
        /// <param name="moment">어떤 상황에서 나온 줄인지를 나타내는 태그입니다.</param>
        /// <param name="random">문장과 닉네임을 고를 난수 생성기입니다.</param>
        /// <returns>만들어진 채팅 한 줄입니다. 만들 수 없으면 빈 줄을 반환합니다.</returns>
        public static ChatLine Create(ChatTemplateSet templateSet, EBroadcastMoment moment, Random random)
        {
            if (templateSet == null || random == null) return default;

            string[] lines = templateSet.GetLines(moment);

            if (lines.Length == 0) return default;

            string text = lines[random.Next(lines.Length)];

            if (string.IsNullOrEmpty(text)) return default;

            return new ChatLine(PickNickname(templateSet, random), text);
        }

        /// <summary>
        /// 채팅을 칠 시청자 이름을 고릅니다.
        /// </summary>
        /// <param name="templateSet">닉네임을 가져올 템플릿 세트입니다.</param>
        /// <param name="random">닉네임을 고를 난수 생성기입니다.</param>
        /// <returns>고른 닉네임입니다. 후보가 없으면 번호로 만든 이름을 반환합니다.</returns>
        /// <remarks>
        /// 후보가 비어 있어도 이름 없는 줄을 내보내지 않습니다.
        /// 채팅창에서 누가 썼는지가 보이지 않으면 흐름이 아니라 자막처럼 보입니다.
        /// </remarks>
        private static string PickNickname(ChatTemplateSet templateSet, Random random)
        {
            string[] nicknames = templateSet.Nicknames;

            if (nicknames.Length == 0) return $"시청자{random.Next(1000, 10000)}";

            return nicknames[random.Next(nicknames.Length)];
        }
        #endregion // 함수
    }
}
