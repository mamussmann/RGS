/* 
* Copyright (c) 2024 Marc Mußmann
*
* Permission is hereby granted, free of charge, to any person obtaining a copy of 
* this software and associated documentation files (the "Software"), to deal in the 
* Software without restriction, including without limitation the rights to use, copy, 
* modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
* and to permit persons to whom the Software is furnished to do so, subject to the 
* following conditions:
*
* The above copyright notice and this permission notice shall be included in all 
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
* INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
* PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
* FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
* OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
* DEALINGS IN THE SOFTWARE.
*/
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{
    public class UIPieActionItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_label;
        [SerializeField] private Image m_icon;
        [SerializeField] private GameObject m_iconContainer;
        [SerializeField] private Image m_backgroundImage;
        [SerializeField] private Image m_openMarker;
        [SerializeField] private float m_markerDistance;
        [SerializeField] private Color m_highlightColor;
        [SerializeField] private Color m_markerColor;
        [SerializeField] private Color m_isPathColor;
        private bool m_isOnFocusLayer;
        private void Awake() {
            m_openMarker.color = m_markerColor;
        }
        public void SetContent(string text, Sprite iconSprite)
        {
            m_label.text = text;
            m_icon.sprite = iconSprite;
            m_icon.gameObject.SetActive(iconSprite != null);
        }
        public void IsGoToIndex(bool value, float angle)
        {
            m_openMarker.transform.localRotation = Quaternion.Euler(0,0,-45 + (-Mathf.Rad2Deg * angle));
            m_openMarker.gameObject.SetActive(value);
        }
        public void ShowHightlight()
        {
            m_backgroundImage.color = m_highlightColor;
            transform.localScale = Vector3.one * 1.1f;
        }
        
        public void HideHightlight()
        {
            m_backgroundImage.color = m_isOnFocusLayer ? Color.white : Color.gray;
            transform.localScale = Vector3.one * 1.0f;
        }

        public void SetIsFocusLayer(bool value)
        {
            m_isOnFocusLayer = value;
            m_backgroundImage.color = m_isOnFocusLayer ? Color.white : Color.gray;
            transform.localScale = value ? Vector3.one * 1.0f : Vector3.one * 0.9f;
        }

        internal void SetInteractable(bool isInteractable)
        {
            Color color = Color.gray * 0.5f;
            color.a = 0.5f;
            m_backgroundImage.color = isInteractable ? m_backgroundImage.color : color;
        }

        internal void SetIsPath(bool isPath)
        {
            m_backgroundImage.color = isPath ? m_isPathColor : m_backgroundImage.color;
        }
    }

}
