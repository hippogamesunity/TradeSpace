﻿using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Engine;

namespace Assets.Scripts.Views
{
    public class WarehouseView : BaseWarehouseView
    {
        public void Start()
        {
            GetButton.Up += Get;
            PutButton.Up += Put;
        }

        protected override void SyncItems()
        {
            var location = SelectManager.Location.Name;

            if (!Profile.Instance.Warehouses.ContainsKey(location))
            {
                Profile.Instance.Warehouses.Add(location, new MemoWarehouse());
            }

            ShopItems = Profile.Instance.Warehouses[location].Goods.Select(i => GetShopItem(i)).ToDictionary(i => i.Id.String);
            ShipItems = Profile.Instance.Ship.Goods.Select(i => GetShopItem(i)).ToDictionary(i => i.Id.String);
        }

        protected override void SyncItemsBack()
        {
            var location = SelectManager.Location.Name;

            Profile.Instance.Warehouses[location].Goods = ShopItems.Values.Select(i => GetMemoGoods(i)).ToList();
            Profile.Instance.Ship.Goods = ShipItems.Values.Select(i => GetMemoGoods(i)).ToList();

            foreach (var item in Profile.Instance.Ship.Goods)
            {
                item.Price = 0;
            }
        }
    }
}