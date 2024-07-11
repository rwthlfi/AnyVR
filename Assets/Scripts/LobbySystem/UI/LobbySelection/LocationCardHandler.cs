using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI
{
    public class LocationCardHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel, _descriptionLabel;

        [SerializeField] private Image _thumbnail;
        public LobbySceneMetaData MetaData { get; private set; }
        public event Action Click;

        internal void SetMetaData(LobbySceneMetaData meta)
        {
            MetaData = meta;
            _nameLabel.text = meta.name;
            _descriptionLabel.text = meta.Description;
            _thumbnail.sprite = meta.Thumbnail;
        }

        public void OnBtnClick()
        {
            Click?.Invoke();
        }
    }
}