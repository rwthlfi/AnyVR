using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI.LobbySelection
{
    public class LocationCardHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel, _descriptionLabel;

        [SerializeField] private Image _thumbnail;
        public LobbySceneMetaData MetaData { get; private set; }
        public event Action Click;

        public void ScaleTo(float value)
        {
            LeanTween.scale(gameObject, Vector3.one * value, .1f);
        }

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