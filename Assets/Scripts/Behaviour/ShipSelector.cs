﻿using Assets.Scripts.Common;
using Assets.Scripts.Engine;
using Assets.Scripts.Enums;
using Assets.Scripts.Views;
using UnityEngine;

namespace Assets.Scripts.Behaviour
{
    public class ShipSelector : Script
    {
        public SelectButton Button;
        public UISprite State;
        public UISprite Mass;
        public UISprite Volume;

        private int _index;

        public void Initialize(int index)
        {
            _index = index;

            Button.Selected += () => SelectManager.SelectShip(index);

            if (Profile.Instance.SelectedShip == index)
            {
                Button.Pressed = true;
            }

            var ship = new PlayerShip(Profile.Instance.Ships[_index]);

            Mass.transform.localScale = new Vector2(1, (float) ship.GoodsMass / ship.Mass);
            Volume.transform.localScale = new Vector2(1, (float) ship.GoodsVolume / ship.Volume);
        }

        public void Update()
        {
            switch (ShipsView.Ships[_index].State)
            {
                case ShipState.InFlight:
                    State.color = Color.yellow;
                    break;
                default:
                    State.color = Color.green;
                    break;
            }
        }
    }
}