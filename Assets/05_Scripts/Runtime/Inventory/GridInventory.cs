using System;
using System.Collections.Generic;

namespace ProjectR.Inventory
{
    /// <summary>
    /// 칸 기반 격자 인벤토리의 계산부입니다.
    /// </summary>
    /// <remarks>
    /// 유니티 타입을 하나도 쓰지 않아 에디트 모드 테스트로 규칙을 그대로 검증할 수 있습니다.
    /// 화면에 어떻게 보이는지는 UI가 정하고, 여기서는 어디에 놓을 수 있는지만 판정합니다.
    /// 무게 개념은 넣지 않습니다. 관리 부담만 늘어나기 때문입니다.
    /// </remarks>
    public sealed class GridInventory
    {
        #region 필드
        /// <summary>칸마다 놓여 있는 물건의 실체 번호입니다. 비어 있으면 0입니다.</summary>
        private readonly int[] cells;

        /// <summary>실체 번호로 물건을 찾기 위한 표입니다.</summary>
        private readonly Dictionary<int, PlacedItem> itemsById = new Dictionary<int, PlacedItem>();

        /// <summary>넣은 순서대로 담아 둔 물건 목록입니다.</summary>
        private readonly List<PlacedItem> items = new List<PlacedItem>();

        /// <summary>다음에 매길 실체 번호입니다.</summary>
        private int nextInstanceId = 1;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>가로 칸 수입니다.</summary>
        public int Width { get; }

        /// <summary>세로 칸 수입니다.</summary>
        public int Height { get; }

        /// <summary>전체 칸 수입니다.</summary>
        public int CellCount => Width * Height;

        /// <summary>물건이 차지하고 있는 칸 수입니다.</summary>
        public int OccupiedCellCount { get; private set; }

        /// <summary>담겨 있는 물건 목록입니다. 없으면 빈 목록입니다.</summary>
        public IReadOnlyList<PlacedItem> Items => items;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 격자 인벤토리를 만듭니다. 가로세로는 최소 1칸으로 맞춰집니다.
        /// </summary>
        /// <param name="width">가로 칸 수입니다.</param>
        /// <param name="height">세로 칸 수입니다.</param>
        public GridInventory(int width, int height)
        {
            Width = width < 1 ? 1 : width;
            Height = height < 1 ? 1 : height;
            cells = new int[Width * Height];
        }

        /// <summary>
        /// 지정한 자리에 형태를 놓을 수 있는지 판정합니다.
        /// </summary>
        /// <param name="shape">놓을 형태입니다.</param>
        /// <param name="position">왼쪽 위 칸 좌표입니다.</param>
        /// <param name="isRotated">90도 돌려 놓을지 여부입니다.</param>
        /// <param name="ignoreInstanceId">겹쳐도 무시할 물건의 실체 번호입니다. 없으면 0을 넘깁니다.</param>
        /// <returns>놓을 수 있으면 true를 반환합니다.</returns>
        /// <remarks>
        /// 물건을 옮길 때는 자기 자신이 놓여 있던 칸과 겹치므로 <paramref name="ignoreInstanceId"/>가 필요합니다.
        /// </remarks>
        public bool CanPlace(InventoryShape shape, GridPosition position, bool isRotated, int ignoreInstanceId = 0)
        {
            int width = shape.GetWidth(isRotated);
            int height = shape.GetHeight(isRotated);

            if (position.X < 0 || position.Y < 0) return false;
            if (position.X + width > Width || position.Y + height > Height) return false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int occupant = cells[GetCellIndex(position.X + x, position.Y + y)];

                    if (occupant != 0 && occupant != ignoreInstanceId) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 지정한 자리에 물건을 놓습니다.
        /// </summary>
        /// <param name="definitionId">물건 정의를 가리키는 식별자입니다.</param>
        /// <param name="shape">물건이 차지하는 형태입니다.</param>
        /// <param name="position">왼쪽 위 칸 좌표입니다.</param>
        /// <param name="isRotated">90도 돌려 놓을지 여부입니다.</param>
        /// <param name="placed">놓인 물건입니다. 실패하면 null입니다.</param>
        /// <returns>놓았으면 true를 반환합니다.</returns>
        public bool TryPlaceAt(string definitionId, InventoryShape shape, GridPosition position,
            bool isRotated, out PlacedItem placed)
        {
            placed = null;

            if (CanPlace(shape, position, isRotated) == false) return false;

            placed = new PlacedItem(nextInstanceId++, definitionId, shape, position, isRotated);

            itemsById.Add(placed.InstanceId, placed);
            items.Add(placed);
            Occupy(placed);

            return true;
        }

        /// <summary>
        /// 빈자리를 스스로 찾아 물건을 넣습니다.
        /// </summary>
        /// <param name="definitionId">물건 정의를 가리키는 식별자입니다.</param>
        /// <param name="shape">물건이 차지하는 형태입니다.</param>
        /// <param name="placed">놓인 물건입니다. 실패하면 null입니다.</param>
        /// <returns>넣었으면 true를 반환합니다.</returns>
        /// <remarks>
        /// 위에서 아래로, 왼쪽에서 오른쪽으로 훑어 처음 들어가는 자리에 넣습니다.
        /// 돌리지 않은 방향을 먼저 시도하므로 같은 물건은 되도록 같은 방향으로 들어갑니다.
        /// </remarks>
        public bool TryAdd(string definitionId, InventoryShape shape, out PlacedItem placed)
        {
            if (TryAddInDirection(definitionId, shape, false, out placed)) return true;
            if (shape.IsSquare) return false;

            return TryAddInDirection(definitionId, shape, true, out placed);
        }

        /// <summary>
        /// 이미 놓인 물건을 다른 자리로 옮기거나 돌립니다.
        /// </summary>
        /// <param name="instanceId">옮길 물건의 실체 번호입니다.</param>
        /// <param name="position">새 왼쪽 위 칸 좌표입니다.</param>
        /// <param name="isRotated">새 회전 여부입니다.</param>
        /// <returns>옮겼으면 true를 반환합니다. 놓을 수 없으면 원래 자리를 그대로 둡니다.</returns>
        public bool TryMove(int instanceId, GridPosition position, bool isRotated)
        {
            if (itemsById.TryGetValue(instanceId, out PlacedItem item) == false) return false;
            if (CanPlace(item.Shape, position, isRotated, instanceId) == false) return false;

            Release(item);
            item.MoveTo(position, isRotated);
            Occupy(item);

            return true;
        }

        /// <summary>
        /// 물건을 인벤토리에서 빼냅니다.
        /// </summary>
        /// <param name="instanceId">빼낼 물건의 실체 번호입니다.</param>
        /// <returns>빼냈으면 true를 반환합니다.</returns>
        public bool Remove(int instanceId)
        {
            if (itemsById.TryGetValue(instanceId, out PlacedItem item) == false) return false;

            Release(item);
            itemsById.Remove(instanceId);
            items.Remove(item);

            return true;
        }

        /// <summary>
        /// 지정한 칸에 놓여 있는 물건을 찾습니다.
        /// </summary>
        /// <param name="position">확인할 칸 좌표입니다.</param>
        /// <returns>그 칸의 물건입니다. 비어 있으면 null을 반환합니다.</returns>
        public PlacedItem GetAt(GridPosition position)
        {
            if (position.X < 0 || position.Y < 0) return null;
            if (position.X >= Width || position.Y >= Height) return null;

            int occupant = cells[GetCellIndex(position.X, position.Y)];

            return occupant == 0 ? null : itemsById[occupant];
        }

        /// <summary>
        /// 담겨 있는 물건을 모두 비웁니다.
        /// </summary>
        /// <remarks>탐험 실패로 가방을 통째로 잃을 때 씁니다.</remarks>
        public void Clear()
        {
            for (int i = 0; i < cells.Length; i++) cells[i] = 0;

            itemsById.Clear();
            items.Clear();
            OccupiedCellCount = 0;
        }

        /// <summary>
        /// 한 방향으로만 빈자리를 훑어 물건을 넣습니다.
        /// </summary>
        /// <param name="definitionId">물건 정의를 가리키는 식별자입니다.</param>
        /// <param name="shape">물건이 차지하는 형태입니다.</param>
        /// <param name="isRotated">90도 돌려 놓을지 여부입니다.</param>
        /// <param name="placed">놓인 물건입니다. 실패하면 null입니다.</param>
        /// <returns>넣었으면 true를 반환합니다.</returns>
        private bool TryAddInDirection(string definitionId, InventoryShape shape, bool isRotated,
            out PlacedItem placed)
        {
            for (int y = 0; y <= Height - shape.GetHeight(isRotated); y++)
            {
                for (int x = 0; x <= Width - shape.GetWidth(isRotated); x++)
                {
                    if (TryPlaceAt(definitionId, shape, new GridPosition(x, y), isRotated, out placed)) return true;
                }
            }

            placed = null;

            return false;
        }

        /// <summary>
        /// 물건이 차지하는 칸을 채워 표시합니다.
        /// </summary>
        /// <param name="item">칸을 채울 물건입니다.</param>
        private void Occupy(PlacedItem item)
        {
            ForEachCell(item, index => cells[index] = item.InstanceId);
            OccupiedCellCount += item.Shape.CellCount;
        }

        /// <summary>
        /// 물건이 차지하던 칸을 비웁니다.
        /// </summary>
        /// <param name="item">칸을 비울 물건입니다.</param>
        private void Release(PlacedItem item)
        {
            ForEachCell(item, index => cells[index] = 0);
            OccupiedCellCount -= item.Shape.CellCount;
        }

        /// <summary>
        /// 물건이 차지하는 칸을 하나씩 훑습니다.
        /// </summary>
        /// <param name="item">훑을 물건입니다.</param>
        /// <param name="action">칸 번호마다 실행할 처리입니다.</param>
        private void ForEachCell(PlacedItem item, Action<int> action)
        {
            for (int y = 0; y < item.Height; y++)
            {
                for (int x = 0; x < item.Width; x++)
                {
                    action(GetCellIndex(item.Position.X + x, item.Position.Y + y));
                }
            }
        }

        /// <summary>
        /// 칸 좌표를 1차원 배열 번호로 바꿉니다.
        /// </summary>
        /// <param name="x">가로 방향 칸 번호입니다.</param>
        /// <param name="y">세로 방향 칸 번호입니다.</param>
        /// <returns>칸 배열에서의 번호입니다.</returns>
        private int GetCellIndex(int x, int y)
        {
            return (y * Width) + x;
        }
        #endregion // 함수
    }
}
