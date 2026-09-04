namespace ProjectR.Inventory
{
    /// <summary>
    /// 이상물체가 인벤토리에서 차지하는 직사각형 크기입니다.
    /// </summary>
    /// <remarks>
    /// 프로토타입에서는 L자 같은 꺾인 형태를 쓰지 않습니다.
    /// 정리하는 재미는 1x1, 2x1, 2x3, 1x4처럼 서로 다른 직사각형만으로도 나오고,
    /// 칸 단위 마스크를 도입하면 회전과 충돌 판정이 함께 복잡해지기 때문입니다.
    /// </remarks>
    public readonly struct InventoryShape
    {
        #region 프로퍼티
        /// <summary>회전하지 않았을 때의 가로 칸 수입니다.</summary>
        public int Width { get; }

        /// <summary>회전하지 않았을 때의 세로 칸 수입니다.</summary>
        public int Height { get; }

        /// <summary>이 형태가 차지하는 칸 수입니다.</summary>
        public int CellCount => Width * Height;

        /// <summary>가로세로가 같아 회전해도 모양이 그대로인지 여부입니다.</summary>
        public bool IsSquare => Width == Height;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 형태를 만듭니다. 가로세로는 최소 1칸으로 맞춰집니다.
        /// </summary>
        /// <param name="width">가로 칸 수입니다.</param>
        /// <param name="height">세로 칸 수입니다.</param>
        public InventoryShape(int width, int height)
        {
            Width = width < 1 ? 1 : width;
            Height = height < 1 ? 1 : height;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 회전 여부에 따른 가로 칸 수를 구합니다.
        /// </summary>
        /// <param name="isRotated">90도 돌렸는지 여부입니다.</param>
        /// <returns>실제로 차지하는 가로 칸 수입니다.</returns>
        public int GetWidth(bool isRotated)
        {
            return isRotated ? Height : Width;
        }

        /// <summary>
        /// 회전 여부에 따른 세로 칸 수를 구합니다.
        /// </summary>
        /// <param name="isRotated">90도 돌렸는지 여부입니다.</param>
        /// <returns>실제로 차지하는 세로 칸 수입니다.</returns>
        public int GetHeight(bool isRotated)
        {
            return isRotated ? Width : Height;
        }

        /// <summary>
        /// 형태를 읽기 좋은 문자열로 만듭니다.
        /// </summary>
        /// <returns>가로x세로 형태의 문자열입니다.</returns>
        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
        #endregion // 함수
    }
}
