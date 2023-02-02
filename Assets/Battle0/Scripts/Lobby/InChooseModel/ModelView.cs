﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Altzone.Scripts;
using Altzone.Scripts.Model;
using Altzone.Scripts.Model.Dto;
using UnityEngine;
using UnityEngine.UI;

namespace Battle0.Scripts.Lobby.InChooseModel
{
    /// <summary>
    /// <c>CharacterModel</c> view - example using furniture prefabs as we do not have proper player prefabs for character selection yet.
    /// </summary>
    public class ModelView : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private InputField playerName;
        [SerializeField] private Button continueButton;

        [SerializeField] private Transform leftPane;
        [SerializeField] private Transform rightPane;
        [SerializeField] private Transform _prefabsRoot;
        [SerializeField] private GameObject[] _prefabs;
        [SerializeField] private GameObject _curPrefab;
        [SerializeField] private bool _isReady;

        private Button[] _buttons;
        private Text[] _labels;

        public bool IsReady => _isReady;

        private void Awake()
        {
            _buttons = leftPane.GetComponentsInChildren<Button>();
            _labels = rightPane.GetComponentsInChildren<Text>();
            LoadPrefabsAsync();
        }

        private void LoadPrefabsAsync()
        {
            // This will be async if custom character models are loaded from network.
            var store = Storefront.Get();
            var furnitureModels = store.GetAllFurnitureModels();
            var maxIndex = furnitureModels.Max(x => x.Id);
            _prefabs = new GameObject[1 + maxIndex];
            var position = _prefabsRoot.position;
            foreach (var furnitureModel in furnitureModels)
            {
                Debug.Log($"{furnitureModel.Id} {furnitureModel.PrefabName}");
                var instance = FurnitureModel.Instantiate(furnitureModel, position, Quaternion.identity, _prefabsRoot);
                if (instance == null)
                {
                    continue;
                }
                instance.SetActive(false);
                _prefabs[furnitureModel.Id] = instance;
            }
            _isReady = true;
        }

        public string Title
        {
            get => titleText.text;
            set => titleText.text = value;
        }

        public string PlayerName
        {
            get => playerName.text;
            set => playerName.text = value;
        }

        public int CurrentCharacterId { get; private set; }

        public Action ContinueButtonOnClick
        {
            set { continueButton.onClick.AddListener(() => value()); }
        }

        public void Reset()
        {
            Title = string.Empty;
            PlayerName = string.Empty;
            foreach (var label in _labels)
            {
                label.text = string.Empty;
            }
            foreach (var prefab in _prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }
                prefab.SetActive(false);
            }
            foreach (var button in _buttons)
            {
                button.gameObject.SetActive(false);
            }
            _curPrefab = null;
        }

        public void SetCharacters(List<IBattleCharacter> characters, int currentCharacterId)
        {
            Debug.Log($"characters {characters.Count} current {currentCharacterId}");
            CurrentCharacterId = currentCharacterId;
            for (var i = 0; i < characters.Count; ++i)
            {
                var character = characters[i];
                var button = _buttons[i];
                button.gameObject.SetActive(true);
                button.interactable = true;
                button.SetCaption(character.Name);
                button.onClick.AddListener(() =>
                {
                    CurrentCharacterId = character.CustomCharacterModelId;
                    ShowCharacter(character);
                });
                if (currentCharacterId == character.CustomCharacterModelId)
                {
                    ShowCharacter(character);
                }
            }
        }

        private void ShowCharacter(IBattleCharacter character)
        {
            var i = -1;
            var characterName = character.Name == character.CharacterClassName
                ? character.Name
                : $"{character.Name} [{character.CharacterClassName}]";
            _labels[++i].text = $"{characterName}";
            _labels[++i].text = $"MainDefence:\r\n{character.MainDefence}";
            _labels[++i].text = $"Speed:\r\n{character.Speed}";
            _labels[++i].text = $"Resistance:\r\n{character.Resistance}";
            _labels[++i].text = $"Attack:\r\n{character.Attack}";
            _labels[++i].text = $"Defence:\r\n{character.Defence}";
            SetCharacterPrefab(character);
        }

        private void SetCharacterPrefab(IBattleCharacter character)
        {
            if (_curPrefab != null)
            {
                _curPrefab.SetActive(false);
            }
            _curPrefab = _prefabs[character.PlayerPrefabId];
            if (_curPrefab != null)
            {
                _curPrefab.SetActive(true);
            }
        }
    }
}