﻿using System.Collections.Generic;
using Assets.Scripts.Behaviour;
using UnityEngine;

namespace Assets.Scripts.Views
{
    public class ShipsView : ViewBase
    {
        public Transform SelectorPanel;
        public static readonly List<ShipBehaviour> Ships = new List<ShipBehaviour>();
        
        protected override void Initialize()
        {
            for (var i = 0; i < Profile.Instance.Ships.Count; i++)
            {
                var ship = PrefabsHelper.InstantiateShip(Panel).GetComponent<ShipBehaviour>();
                var selector = PrefabsHelper.InstantiateShipSelector(SelectorPanel);

                ship.Initialize(Profile.Instance.Ships[i]);
                Ships.Add(ship);
                selector.transform.localPosition = new Vector2(0, 110 * ((Profile.Instance.Ships.Count - 1) / 2f) - 110 * i);
                selector.GetComponentInChildren<ShipSelector>().Initialize(i);
            }
        }

        protected override void Cleanup()
        {
            Panel.Clean();
            SelectorPanel.Clean();
            Ships.Clear();
        }
    }
}