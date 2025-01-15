// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2024 Engineering Hydrology, RWTH Aachen University.
// 
// AnyVR is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// AnyVR is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AnyVR.
// If not, see <https://www.gnu.org/licenses/>.

using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVr.Samples.LobbySetup
{
    [RequireComponent(typeof(Slider))]
    public class SliderHandler : MonoBehaviour
    {
        private Slider _slider;

        [SerializeField] private GameObject _valueLabel;
        [SerializeField] private TextMeshProUGUI _valueText;

        private Coroutine _hideLabelCoroutine;

        private bool _isInit;

        private void Start()
        {
            _slider = GetComponent<Slider>();
            _slider.onValueChanged.AddListener(OnValueChanged);
            if (_valueLabel == null)
            {
                _valueText = GetComponentInChildren<TextMeshProUGUI>();
            }

            _isInit = _valueLabel != null && _valueText != null;
            _valueLabel.SetActive(false);
        }

        public void OnPointerDown()
        {
            if (_hideLabelCoroutine != null)
            {
                StopCoroutine(_hideLabelCoroutine);
                _hideLabelCoroutine = null;
            }

            _valueLabel.SetActive(true);
        }

        public void OnPointerUp()
        {
            _hideLabelCoroutine ??= StartCoroutine(HideLabel());
        }

        private void OnValueChanged(float value)
        {
            if (_hideLabelCoroutine != null)
            {
                StopCoroutine(_hideLabelCoroutine);
                _hideLabelCoroutine = null;
            }

            if (!_isInit)
            {
                return;
            }

            _valueText.text = value.ToString(CultureInfo.InvariantCulture);
        }

        private IEnumerator HideLabel(float delay = 1f)
        {
            yield return new WaitForSeconds(delay);
            _valueLabel.SetActive(false); // Hide the label after delay
            _hideLabelCoroutine = null; // Reset the coroutine handle
        }
    }
}