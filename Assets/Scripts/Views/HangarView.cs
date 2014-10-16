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
    public class HangarView : ViewBase, IScreenView
    {
        public Transform InstalledTransform;
        public Transform EquipmentTransform;
        public GameButton InstallButton;
        public GameButton RemoveButton;
        public CargoView CargoView;

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

        protected override void Initialize()
        {
            _installed = Profile.Instance.Ship.InstalledEquipment;
            _equipment = Profile.Instance.Ship.Equipment;
            _ship = new PlayerShip(Profile.Instance.Ship);
            _index = _ship.FindFreeSlot();
            _hangarAction = HangarAction.None;

            InstalledTransform.Clean();
            EquipmentTransform.Clean();
            InitializeEquipmentCellButtons();
            Refresh();
            CargoView.Open();
        }

        protected override void Cleanup()
        {
            CargoView.Close();
        }

        public void SelectEquipmentToInstall(EquipmentId equipment)
        {
            _selectedEquipment = equipment;
            _hangarAction = HangarAction.Install;
            RefreshButtons();
        }

        public void SelectEquipmentToRemove(EquipmentId equipment, long index)
        {
            _selectedEquipment = equipment;
            _index = index;
            _hangarAction = HangarAction.Remove;
            RefreshButtons();
        }

        public void SelectEquipmentCell(long index)
        {
            _index = index;
        }

        public void Install()
        {
            var equipment = _equipment.Single(_selectedEquipment);

            if (equipment.Quantity.Long == 1)
            {
                _equipment.Remove(equipment);
            }
            else
            {
                equipment.Quantity.Long--;
            }

            if (_installed.Any(i => i.Index == _index))
            {
                _index = _ship.FindFreeSlot();
            }

            _installed.Add(new MemoInstalledEquipment { Id = equipment.Id, Index = _index });

            Refresh();
            CargoView.Refresh();
        }

        public void Remove()
        {
            _installed.RemoveAll(i => i.Id == _selectedEquipment && i.Index == _index);

            if (_equipment.Contains(_selectedEquipment))
            {
                _equipment.Single(_selectedEquipment).Quantity.Long++;
            }
            else
            {
                _equipment.Add(new MemoEquipment { Id = _selectedEquipment, Quantity = 1.Encrypt() });
            }

            _hangarAction = HangarAction.None;

            Refresh();
            CargoView.Refresh();
        }

        #region Helpers

        private const float AnimationTime = 0.25f;
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
            for (var i = 0; i < _ship.EquipmentSlots; i++)
            {
                var position = new Vector3(-75 * (_ship.EquipmentSlots - 1) + 150 * i, 0);
                var cell = PrefabsHelper.InstantiateEquipmentCellButton(InstalledTransform);
                var index = i;

                cell.GetComponent<SelectButton>().Selected += () => SelectEquipmentCell(index);
                cell.transform.localPosition = position;
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
                var position = new Vector3(-75* (_ship.EquipmentSlots - 1) + 150 * index, 0);
                var button = PrefabsHelper.InstantiateInstalledEquipmentButton(InstalledTransform).GetComponent<InstalledEquipmentButton>();

                button.Initialize(installed.Id, index);
                ShopView.TweenButton(button, position + Shift, 0, 0);
                ShopView.TweenButton(button, position, 1, AnimationTime);
            }
        }

        private void RemoveRedundantEquipmentButtons()
        {
            var buttons = EquipmentTransform.GetComponentsInChildren<EquipmentButton>();

            foreach (var button in buttons.Where(i => _equipment.All(j => j.Id != i.EquipmentId)))
            {
                RemoveEquipmentButton(button.gameObject, -Shift);
            }
        }

        private void InitializeEquipmentButtons()
        {
            var buttons = EquipmentTransform.GetComponentsInChildren<EquipmentButton>();

            for (var i = 0; i < _equipment.Count; i++)
            {
                var button = buttons.FirstOrDefault(j => j.EquipmentId == _equipment[i].Id);
                var position = new Vector3(-75 * (_equipment.Count - 1) + 150 * i, 0);

                if (button == null)
                {
                    button = PrefabsHelper.InstantiateEquipmentButton(EquipmentTransform).GetComponent<EquipmentButton>();
                    button.transform.localPosition = position - Shift;
                    TweenAlpha.Begin(button.gameObject, 0, 0);
                }

                button.Initialize(_equipment[i].Id, _equipment[i].Quantity.Long);
                ShopView.TweenButton(button, position, 1, AnimationTime);
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
            if (SelectManager.Location == null || SelectManager.Ship.Location.Name != SelectManager.Location.Name || !(SelectManager.Location is Station))
            {
                InstallButton.Enabled = RemoveButton.Enabled = false;
            }
            else
            {
                InstallButton.Enabled = _selectedEquipment != EquipmentId.Empty && _ship.HasFreeSlot() && _hangarAction == HangarAction.Install;
                RemoveButton.Enabled = _selectedEquipment != EquipmentId.Empty && _hangarAction == HangarAction.Remove;
            }
        }

        #endregion
    }
}