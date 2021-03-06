﻿using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Behaviour;
using Assets.Scripts.Common;
using Assets.Scripts.Data;
using Assets.Scripts.Engine;
using Assets.Scripts.Enums;
using UnityEngine;

namespace Assets.Scripts.Views
{
    public class Workshop : UIScreen
    {
        public Transform InstalledTransform;
        public Transform EquipmentTransform;
        public GameButton InstallButton;
        public GameButton RemoveButton;
        public UISprite SelectedImage;
        public UILabel SelectedName;

        private List<MemoInstalledEquipment> _installed;
        private List<MemoEquipment> _equipment;
        private PlayerShip _ship;
        private EquipmentId _selectedEquipment;
        private long _index;
        private HangarAction _hangarAction;

        internal enum HangarAction
        {
            None,
            Install,
            Remove
        }

        public void Reload()
        {
            Initialize();
        }

        protected override void Initialize()
        {
            _installed = Profile.Instance.MemoShip.InstalledEquipment;
            _equipment = Profile.Instance.MemoShip.Equipment;
            _ship = Profile.Instance.PlayerShip;
            _index = _ship.HasFreeSlot() ? _ship.FindFreeSlot() : 0;
            _hangarAction = HangarAction.None;
            
            SelectedName.text = SelectedImage.spriteName = null;
            InstalledTransform.Clear();
            EquipmentTransform.Clear();
            InitializeEquipmentCellButtons();
            Refresh();
            Open<Cargo>();
            Close<ShipSelect>(); // TODO: Refact view to support ship switch (see BaseShop for example)
        }

        protected override void Cleanup()
        {
            Close<Cargo>();
            Open<ShipSelect>();
        }

        public void SelectEquipmentToInstall(EquipmentId equipment)
        {
            _selectedEquipment = equipment;
            _hangarAction = HangarAction.Install;

            SelectedImage.spriteName = _selectedEquipment.ToString(); // TODO: cp
            SelectedName.SetText(_selectedEquipment.ToString());

            RefreshButtons();
        }

        public void SelectEquipmentToRemove(EquipmentId equipment, long index)
        {
            _selectedEquipment = equipment;
            _index = index;
            _hangarAction = HangarAction.Remove;

            SelectedImage.spriteName = _selectedEquipment.ToString();
            SelectedName.SetText(_selectedEquipment.ToString());

            RefreshButtons();
        }

        public void SelectEquipmentCell(long index)
        {
            _index = index;
        }

        public void Install()
        {
            var equipment = _equipment.Single(i => i.Id == _selectedEquipment);

            if (!Profile.Instance.PlayerShip.CanInstallEquipment(_selectedEquipment)) // TODO: Disable and mark bad equipment, don't show error
            {
                Find<Dialog>().Open("Error", "Unable to install equipment");
                return;
            }

            if (equipment.Quantity.Long == 1)
            {
                _equipment.Remove(equipment);
            }
            else
            {
                equipment.Quantity--;
            }

            if (_installed.Any(i => i.Index == _index))
            {
                _index = _ship.FindFreeSlot();
            }

            _installed.Add(new MemoInstalledEquipment { Id = equipment.Id, Index = _index });

            Refresh();
            GetComponent<Cargo>().Refresh();
        }

        public void Remove()
        {
            _installed.RemoveAll(i => i.Id == _selectedEquipment && i.Index == _index);

            var item = _equipment.FirstOrDefault(i => i.Id == _selectedEquipment);

            if (item != null)
            {
                item.Quantity++;
            }
            else
            {
                _equipment.Add(new MemoEquipment { Id = _selectedEquipment, Quantity = 1 });
            }

            _hangarAction = HangarAction.None;

            Refresh();
            GetComponent<Cargo>().Refresh();
        }

        #region Helpers

        private const float AnimationTime = 0.25f;
        private const float Step = 170;
        private static readonly Vector3 Shift = new Vector3(0, 150);

        private void Refresh()
        {
            RemoveRedundantInstalledEquipmentButtons();
            InitializeInstalledEquipmentButtons();
            RemoveRedundantEquipmentButtons();
            InitializeEquipmentButtons();
            RefreshButtons();
        }

        private void InitializeEquipmentCellButtons()
        {
            for (var i = 0; i < _ship.Equipment; i++)
            {
                var position = new Vector3(-Step / 2 * (_ship.Equipment - 1) + Step * i, 0);
                var cell = PrefabsHelper.InstantiateEquipmentSlotButton(InstalledTransform);
                var index = i;

                cell.transform.FindChild("Button").GetComponent<SelectButton>().Selected += () => SelectEquipmentCell(index);
                cell.transform.localPosition = position;
                cell.transform.localScale *= 0.8f;
            }
        }

        private void RemoveRedundantInstalledEquipmentButtons()
        {
            var buttons = InstalledTransform.GetComponentsInChildren<InstalledEquipmentButton>();

            foreach (var button in buttons.Where(b => !_installed.Any(i => i.Id == b.EquipmentId && i.Index == b.Index)))
            {
                RemoveEquipmentButton(button.gameObject, Shift);
            }
        }

        private void InitializeInstalledEquipmentButtons()
        {
            var buttons = InstalledTransform.GetComponentsInChildren<InstalledEquipmentButton>();

            foreach (var installed in _installed)
            {
                if (buttons.Any(i => i.EquipmentId == installed.Id && i.Index == installed.Index)) continue;

                var index = installed.Index;
                var position = new Vector3(-Step / 2 * (_ship.Equipment - 1) + Step * index, 0);
                var button = PrefabsHelper.InstantiateInstalledEquipmentButton(InstalledTransform).GetComponent<InstalledEquipmentButton>();

                button.Initialize(installed.Id, index);
                BaseShop.TweenButton(button, position + Shift, 0, 0);
                BaseShop.TweenButton(button, position, 1, AnimationTime);
            }
        }

        private void RemoveRedundantEquipmentButtons()
        {
            var buttons = EquipmentTransform.GetComponentsInChildren<ShopItemButton>();

            foreach (var button in buttons.Where(i => _equipment.All(j => j.Id != i.EquipmentId)))
            {
                RemoveEquipmentButton(button.gameObject, -Shift);
            }
        }

        private void InitializeEquipmentButtons()
        {
            var buttons = EquipmentTransform.GetComponentsInChildren<ShopItemButton>();

            for (var i = 0; i < _equipment.Count; i++)
            {
                var equipmentId = _equipment[i].Id;
                var button = buttons.FirstOrDefault(j => j.EquipmentId == equipmentId);
                var position = new Vector3(-75 * (_equipment.Count - 1) + 150 * i, 0);

                if (button == null)
                {
                    button = PrefabsHelper.InstantiateEquipmentButton(EquipmentTransform).GetComponent<ShopItemButton>();
                    button.transform.localPosition = position - Shift;
                    //TweenAlpha.Begin(button.gameObject, 0, 0);
                }

                button.Initialize(equipmentId.ToString(), equipmentId.ToString(), () => SelectEquipmentToInstall(equipmentId), _equipment[i].Quantity);
                BaseShop.TweenButton(button, position, 1, AnimationTime);
            }
        }

        private static void RemoveEquipmentButton(GameObject button, Vector3 shift)
        {
            button.GetComponentInChildren<SelectButton>().Pressed = false;
            TweenPosition.Begin(button, AnimationTime, button.transform.localPosition + shift);
            TweenAlpha.Begin(button, AnimationTime, 0);
            Destroy(button, AnimationTime);
        }

        private void RefreshButtons()
        {
            if (SelectManager.Location == null || SelectManager.Ship.Location.Name != SelectManager.Location.Name || !(SelectManager.Location is Data.Station))
            {
                InstallButton.Enabled = RemoveButton.Enabled = false;
            }
            else
            {
                InstallButton.Enabled = _selectedEquipment != EquipmentId.Empty
                    && _equipment.Count(i => i.Id == _selectedEquipment) > 0
                    && _ship.HasFreeSlot()
                    && _hangarAction == HangarAction.Install;
                RemoveButton.Enabled = _selectedEquipment != EquipmentId.Empty
                    && _installed.Count(i => i.Id == _selectedEquipment) > 0
                    && _hangarAction == HangarAction.Remove;
            }
        }

        #endregion
    }
}