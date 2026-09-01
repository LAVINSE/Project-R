using System;
using System.Collections.Generic;

using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectR.Activity;

namespace ProjectR.Broadcast
{
    /// <summary>
    /// 상황 태그마다 쓸 채팅 문장 후보를 모아 둔 정의 에셋입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 6.3절이 정한 방식입니다. 상황 태그와 템플릿을 조합해서 문장을 만듭니다.
    /// 실행 중에 문장을 생성하지 않으므로 비용이 없고, 오프라인에서 돌고,
    /// 스팀의 AI 사용 공개 신고 대상에서도 벗어납니다.
    /// <para>
    /// 문장 품질보다 <b>흐름이 반응하는 느낌</b>이 중요합니다. 그래서 태그마다 문장이 몇 개만 있어도
    /// 상황이 바뀔 때 채팅이 함께 바뀌는 것으로 대부분의 체감이 나옵니다.
    /// </para>
    /// <para>
    /// 태그를 열거형이 아니라 목록으로 담는 이유는 인스펙터에서 태그마다 접었다 펼 수 있어야
    /// 문장이 수십 개가 되어도 다룰 수 있기 때문입니다.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "ChatTemplateSet", menuName = "프로젝트R/채팅 템플릿 세트")]
    public class ChatTemplateSet : SWScriptableObject
    {
        #region 타입
        /// <summary>
        /// 상황 태그 하나에 딸린 문장 후보 묶음입니다.
        /// </summary>
        [Serializable]
        public class MomentTemplates
        {
            #region 필드
            [SerializeField, Tooltip("이 문장들이 쓰이는 상황 태그입니다.")]
            private EBroadcastMoment moment;

            [SerializeField, TextArea, Tooltip("이 상황에서 올라올 문장 후보입니다.")]
            private string[] lines = Array.Empty<string>();
            #endregion // 필드

            #region 프로퍼티
            /// <summary>이 문장들이 쓰이는 상황 태그입니다.</summary>
            public EBroadcastMoment Moment => moment;

            /// <summary>이 상황에서 올라올 문장 후보입니다.</summary>
            public string[] Lines => lines ?? Array.Empty<string>();
            #endregion // 프로퍼티
        }
        #endregion // 타입

        #region 필드
        [SWGroup("시청자")]
        [SerializeField, Tooltip("채팅을 치는 시청자 이름 후보입니다.")]
        private string[] nicknames = Array.Empty<string>();

        [SWGroup("문장")]
        [SerializeField, Tooltip("상황 태그마다의 문장 후보입니다.")]
        private List<MomentTemplates> templates = new List<MomentTemplates>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>채팅을 치는 시청자 이름 후보입니다. 없으면 빈 배열입니다.</summary>
        public string[] Nicknames => nicknames ?? Array.Empty<string>();
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 상황 태그에 딸린 문장 후보를 가져옵니다.
        /// </summary>
        /// <param name="moment">문장을 찾을 상황 태그입니다.</param>
        /// <returns>그 상황의 문장 후보입니다. 등록되지 않았으면 빈 배열을 반환합니다.</returns>
        public string[] GetLines(EBroadcastMoment moment)
        {
            if (templates == null) return Array.Empty<string>();

            for (int i = 0; i < templates.Count; i++)
            {
                if (templates[i] == null) continue;
                if (templates[i].Moment != moment) continue;

                return templates[i].Lines;
            }

            return Array.Empty<string>();
        }
        #endregion // 함수
    }
}
