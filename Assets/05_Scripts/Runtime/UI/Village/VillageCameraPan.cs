using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;

using SW.Attributes;
using SW.Base;

namespace ProjectR.UI.Village
{
    /// <summary>
    /// 마을을 내려다보는 카메라를 끌어서 움직이는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 마을이 한 화면에 다 들어오지 않아도 되게 만드는 장치입니다(기획서 12.7절).
    /// 한 화면에 맞추려고 카메라를 뒤로 빼면 건물이 작아져 무엇이 무엇인지 알아볼 수 없습니다.
    /// 넓게 두고 훑어보게 하는 편이 도시로 읽힙니다.
    /// <para>
    /// 카메라를 회전시키지 않고 <b>바라보는 방향을 고정한 채 평행 이동만</b> 합니다.
    /// 회전을 허용하면 건물 뒤가 보이고, 뒤를 보여 줄 생각으로 만들지 않은 배치가 드러납니다.
    /// 니케 전초기지도 같은 이유로 시점을 고정합니다.
    /// </para>
    /// <para>
    /// 움직일 수 있는 범위를 네모로 묶어 둡니다. 묶지 않으면 도시 밖 허공으로 나가
    /// 아무것도 없는 화면을 보게 됩니다.
    /// </para>
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.UI.Home", sourceAssembly: "ProjectR.UI", sourceClassName: "VillageCameraPan")]
    public class VillageCameraPan : SWMonoBehaviour
    {
        #region 필드
        /// <summary>움직일 카메라입니다. 비우면 자기 자신에 붙은 것을 씁니다.</summary>
        [SWGroup("대상")]
        [SerializeField, Tooltip("움직일 카메라입니다. 비우면 자기 자신에 붙은 것을 씁니다.")]
        private Camera targetCamera;

        /// <summary>마우스를 1픽셀 끌 때 카메라가 움직이는 거리입니다.</summary>
        [SWGroup("이동")]
        [SerializeField, Min(0f), Tooltip("마우스를 1픽셀 끌 때 카메라가 움직이는 거리입니다.")]
        private float dragSpeed = 0.22f;

        /// <summary>끄는 방향과 화면이 움직이는 방향을 반대로 둘지 여부입니다.</summary>
        [SerializeField, Tooltip("끄는 방향과 화면이 움직이는 방향을 반대로 둘지 여부입니다.")]
        private bool invertDrag = true;

        /// <summary>손을 뗀 뒤 미끄러지다 멈추는 데 걸리는 정도입니다. 0이면 바로 멈춥니다.</summary>
        [SerializeField, Min(0f), Tooltip("손을 뗀 뒤 미끄러지다 멈추는 데 걸리는 정도입니다. 0이면 바로 멈춥니다.")]
        private float glide = 8f;

        /// <summary>카메라가 머물 수 있는 가로 범위입니다. x가 최소, y가 최대입니다.</summary>
        [SWGroup("범위")]
        [SerializeField, Tooltip("카메라가 머물 수 있는 가로 범위입니다. x가 최소, y가 최대입니다.")]
        private Vector2 limitX = new(-90f, 90f);

        /// <summary>카메라가 머물 수 있는 세로 범위입니다. x가 최소, y가 최대입니다.</summary>
        [SerializeField, Tooltip("카메라가 머물 수 있는 세로 범위입니다. x가 최소, y가 최대입니다.")]
        private Vector2 limitZ = new(-150f, 60f);

        /// <summary>지금 끌고 있는지 여부입니다.</summary>
        private bool isDragging;

        /// <summary>지난 프레임의 포인터 위치입니다.</summary>
        private Vector2 lastPointer;

        /// <summary>손을 뗀 뒤 남은 속도입니다.</summary>
        private Vector3 velocity;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 움직일 카메라를 정합니다.
        /// </summary>
        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
        }

        /// <summary>
        /// 포인터를 보고 카메라를 옮깁니다.
        /// </summary>
        private void Update()
        {
            if (targetCamera == null) return;

            ReadDrag();
            ApplyGlide();
        }

        /// <summary>
        /// 마우스를 끌고 있으면 그만큼 카메라를 옮깁니다.
        /// </summary>
        /// <remarks>
        /// 화면 평면이 아니라 <b>바닥 평면</b>을 따라 옮깁니다.
        /// 화면 평면으로 옮기면 위아래로 끌 때 카메라가 하늘로 떠오릅니다.
        /// </remarks>
        private void ReadDrag()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastPointer = mouse.position.ReadValue();
                return;
            }

            if (mouse.leftButton.isPressed == false)
            {
                isDragging = false;
                return;
            }

            if (isDragging == false) return;

            Vector2 pointer = mouse.position.ReadValue();
            Vector2 delta = pointer - lastPointer;
            lastPointer = pointer;

            if (delta.sqrMagnitude <= 0f) return;

            // 카메라가 바라보는 방향을 바닥에 눕혀 앞뒤 축을 만듭니다.
            Vector3 forward = Vector3.ProjectOnPlane(targetCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = targetCamera.transform.right;

            float sign = invertDrag ? -1f : 1f;
            Vector3 move = (right * delta.x + forward * delta.y) * dragSpeed * sign;

            MoveBy(move);
            velocity = move / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        /// <summary>
        /// 손을 뗀 뒤 남은 속도로 조금 더 미끄러집니다.
        /// </summary>
        private void ApplyGlide()
        {
            if (isDragging || glide <= 0f) return;
            if (velocity.sqrMagnitude < 0.01f) return;

            MoveBy(velocity * Time.deltaTime);
            velocity = Vector3.Lerp(velocity, Vector3.zero, glide * Time.deltaTime);
        }

        /// <summary>
        /// 카메라를 옮기고 범위 안으로 잘라 냅니다.
        /// </summary>
        /// <param name="move">옮길 거리입니다.</param>
        private void MoveBy(Vector3 move)
        {
            Vector3 next = targetCamera.transform.position + move;

            next.x = Mathf.Clamp(next.x, limitX.x, limitX.y);
            next.z = Mathf.Clamp(next.z, limitZ.x, limitZ.y);
            next.y = targetCamera.transform.position.y;

            targetCamera.transform.position = next;
        }
        #endregion // 함수
    }
}
