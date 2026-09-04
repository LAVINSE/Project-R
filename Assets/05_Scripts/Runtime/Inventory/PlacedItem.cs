namespace ProjectR.Inventory
{
    /// <summary>
    /// 격자 인벤토리에 놓여 있는 물건 하나입니다.
    /// </summary>
    /// <remarks>
    /// 같은 정의를 가진 물건을 여러 개 넣을 수 있어야 하므로
    /// 정의 식별자와 별도로 인벤토리가 매기는 실체 번호를 갖습니다.
    /// 위치와 회전은 <see cref="GridInventory"/>만 바꿀 수 있습니다.
    /// </remarks>
    public sealed class PlacedItem
    {
        #region 프로퍼티
        /// <summary>인벤토리가 매긴 실체 번호입니다. 1부터 시작합니다.</summary>
        public int InstanceId { get; }

        /// <summary>물건 정의를 가리키는 식별자입니다.</summary>
        public string DefinitionId { get; }

        /// <summary>물건이 차지하는 형태입니다.</summary>
        public InventoryShape Shape { get; }

        /// <summary>물건의 왼쪽 위 칸 좌표입니다.</summary>
        public GridPosition Position { get; private set; }

        /// <summary>물건을 90도 돌려 놓았는지 여부입니다.</summary>
        public bool IsRotated { get; private set; }

        /// <summary>지금 차지하고 있는 가로 칸 수입니다.</summary>
        public int Width => Shape.GetWidth(IsRotated);

        /// <summary>지금 차지하고 있는 세로 칸 수입니다.</summary>
        public int Height => Shape.GetHeight(IsRotated);
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 놓인 물건을 만듭니다.
        /// </summary>
        /// <param name="instanceId">인벤토리가 매긴 실체 번호입니다.</param>
        /// <param name="definitionId">물건 정의를 가리키는 식별자입니다.</param>
        /// <param name="shape">물건이 차지하는 형태입니다.</param>
        /// <param name="position">왼쪽 위 칸 좌표입니다.</param>
        /// <param name="isRotated">90도 돌려 놓았는지 여부입니다.</param>
        internal PlacedItem(int instanceId, string definitionId, InventoryShape shape,
            GridPosition position, bool isRotated)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            Shape = shape;
            Position = position;
            IsRotated = isRotated;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 놓인 자리와 회전을 바꿉니다.
        /// </summary>
        /// <param name="position">새 왼쪽 위 칸 좌표입니다.</param>
        /// <param name="isRotated">새 회전 여부입니다.</param>
        internal void MoveTo(GridPosition position, bool isRotated)
        {
            Position = position;
            IsRotated = isRotated;
        }
        #endregion // 함수
    }
}
