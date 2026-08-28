using NUnit.Framework;

using ProjectR.Inventory;

namespace ProjectR.Tests
{
    /// <summary>
    /// 격자 인벤토리 계산부의 배치 규칙을 검증하는 테스트입니다.
    /// </summary>
    /// <remarks>
    /// 계산부가 유니티 타입을 쓰지 않으므로 씬도 플레이 모드도 없이 규칙만 확인할 수 있습니다.
    /// </remarks>
    public class GridInventoryTests
    {
        #region 함수
        /// <summary>
        /// 빈 인벤토리에 넣으면 왼쪽 위부터 채워지는지 확인합니다.
        /// </summary>
        [Test]
        public void 빈_가방에_넣으면_왼쪽_위에_놓인다()
        {
            GridInventory inventory = new GridInventory(6, 4);

            bool isAdded = inventory.TryAdd("a", new InventoryShape(2, 2), out PlacedItem placed);

            Assert.IsTrue(isAdded);
            Assert.AreEqual(0, placed.Position.X);
            Assert.AreEqual(0, placed.Position.Y);
            Assert.AreEqual(4, inventory.OccupiedCellCount);
        }

        /// <summary>
        /// 이미 찬 칸에는 겹쳐 놓을 수 없는지 확인합니다.
        /// </summary>
        [Test]
        public void 이미_찬_칸에는_겹쳐_놓지_못한다()
        {
            GridInventory inventory = new GridInventory(6, 4);

            inventory.TryPlaceAt("a", new InventoryShape(2, 2), new GridPosition(0, 0), false, out _);

            bool canPlace = inventory.CanPlace(new InventoryShape(1, 1), new GridPosition(1, 1), false);

            Assert.IsFalse(canPlace);
        }

        /// <summary>
        /// 격자 밖으로 나가는 자리를 막는지 확인합니다.
        /// </summary>
        [Test]
        public void 격자_밖으로_나가면_놓지_못한다()
        {
            GridInventory inventory = new GridInventory(6, 4);

            Assert.IsFalse(inventory.CanPlace(new InventoryShape(1, 4), new GridPosition(0, 1), false));
            Assert.IsFalse(inventory.CanPlace(new InventoryShape(1, 1), new GridPosition(6, 0), false));
            Assert.IsFalse(inventory.CanPlace(new InventoryShape(1, 1), new GridPosition(-1, 0), false));
        }

        /// <summary>
        /// 돌리면 남은 자리에 들어가는지 확인합니다.
        /// </summary>
        [Test]
        public void 세워서는_안_들어가도_눕히면_들어간다()
        {
            GridInventory inventory = new GridInventory(4, 2);
            InventoryShape longShape = new InventoryShape(1, 4);

            Assert.IsFalse(inventory.CanPlace(longShape, new GridPosition(0, 0), false));
            Assert.IsTrue(inventory.CanPlace(longShape, new GridPosition(0, 0), true));
        }

        /// <summary>
        /// 자동 배치가 필요하면 스스로 돌려 넣는지 확인합니다.
        /// </summary>
        [Test]
        public void 자동_배치는_필요하면_돌려서_넣는다()
        {
            GridInventory inventory = new GridInventory(4, 2);

            bool isAdded = inventory.TryAdd("long", new InventoryShape(1, 4), out PlacedItem placed);

            Assert.IsTrue(isAdded);
            Assert.IsTrue(placed.IsRotated);
            Assert.AreEqual(4, placed.Width);
            Assert.AreEqual(1, placed.Height);
        }

        /// <summary>
        /// 자리가 없으면 넣지 못하는지 확인합니다.
        /// </summary>
        [Test]
        public void 자리가_없으면_넣지_못한다()
        {
            GridInventory inventory = new GridInventory(2, 2);

            Assert.IsTrue(inventory.TryAdd("a", new InventoryShape(2, 2), out _));
            Assert.IsFalse(inventory.TryAdd("b", new InventoryShape(1, 1), out _));
            Assert.AreEqual(1, inventory.Items.Count);
        }

        /// <summary>
        /// 옮길 때 자기 자신이 놓여 있던 칸과는 겹쳐도 되는지 확인합니다.
        /// </summary>
        [Test]
        public void 자기_자리와_겹치게_옮길_수_있다()
        {
            GridInventory inventory = new GridInventory(4, 4);

            inventory.TryPlaceAt("a", new InventoryShape(2, 2), new GridPosition(0, 0), false,
                out PlacedItem placed);

            Assert.IsTrue(inventory.TryMove(placed.InstanceId, new GridPosition(1, 1), false));
            Assert.AreEqual(1, placed.Position.X);
            Assert.AreEqual(1, placed.Position.Y);
            Assert.AreEqual(4, inventory.OccupiedCellCount);
        }

        /// <summary>
        /// 옮길 수 없는 자리로 옮기면 원래 자리에 남는지 확인합니다.
        /// </summary>
        [Test]
        public void 옮길_수_없으면_원래_자리에_남는다()
        {
            GridInventory inventory = new GridInventory(4, 4);

            inventory.TryPlaceAt("a", new InventoryShape(2, 2), new GridPosition(0, 0), false,
                out PlacedItem moving);
            inventory.TryPlaceAt("b", new InventoryShape(2, 2), new GridPosition(2, 0), false, out _);

            Assert.IsFalse(inventory.TryMove(moving.InstanceId, new GridPosition(2, 0), false));
            Assert.AreEqual(0, moving.Position.X);
            Assert.AreEqual(0, moving.Position.Y);
            Assert.AreEqual(8, inventory.OccupiedCellCount);
        }

        /// <summary>
        /// 빼낸 자리가 다시 비는지 확인합니다.
        /// </summary>
        [Test]
        public void 빼내면_자리가_다시_빈다()
        {
            GridInventory inventory = new GridInventory(2, 2);

            inventory.TryAdd("a", new InventoryShape(2, 2), out PlacedItem placed);

            Assert.IsTrue(inventory.Remove(placed.InstanceId));
            Assert.AreEqual(0, inventory.OccupiedCellCount);
            Assert.IsTrue(inventory.TryAdd("b", new InventoryShape(2, 2), out _));
        }

        /// <summary>
        /// 같은 종류를 여러 개 넣어도 서로 구분되는지 확인합니다.
        /// </summary>
        [Test]
        public void 같은_종류를_여러_개_넣어도_서로_구분된다()
        {
            GridInventory inventory = new GridInventory(4, 1);

            inventory.TryAdd("a", new InventoryShape(1, 1), out PlacedItem first);
            inventory.TryAdd("a", new InventoryShape(1, 1), out PlacedItem second);

            Assert.AreNotEqual(first.InstanceId, second.InstanceId);
            Assert.IsTrue(inventory.Remove(first.InstanceId));
            Assert.AreEqual(1, inventory.Items.Count);
            Assert.AreEqual(second.InstanceId, inventory.Items[0].InstanceId);
        }

        /// <summary>
        /// 칸 좌표로 놓인 물건을 찾을 수 있는지 확인합니다.
        /// </summary>
        [Test]
        public void 칸_좌표로_놓인_물건을_찾는다()
        {
            GridInventory inventory = new GridInventory(4, 4);

            inventory.TryPlaceAt("a", new InventoryShape(2, 3), new GridPosition(1, 1), false,
                out PlacedItem placed);

            Assert.AreEqual(placed, inventory.GetAt(new GridPosition(2, 3)));
            Assert.IsNull(inventory.GetAt(new GridPosition(0, 0)));
            Assert.IsNull(inventory.GetAt(new GridPosition(3, 3)));
        }

        /// <summary>
        /// 전부 비우면 가방이 처음 상태로 돌아가는지 확인합니다.
        /// </summary>
        [Test]
        public void 전부_비우면_처음_상태로_돌아간다()
        {
            GridInventory inventory = new GridInventory(4, 4);

            inventory.TryAdd("a", new InventoryShape(2, 2), out _);
            inventory.TryAdd("b", new InventoryShape(1, 4), out _);
            inventory.Clear();

            Assert.AreEqual(0, inventory.Items.Count);
            Assert.AreEqual(0, inventory.OccupiedCellCount);
            Assert.IsTrue(inventory.CanPlace(new InventoryShape(4, 4), new GridPosition(0, 0), false));
        }
        #endregion // 함수
    }
}
