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
using RGS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RGS.Configuration.UI
{
    public class UISceneSelection : MonoBehaviour
    {
        [SerializeField] private GameObject m_entryPrefab;
        [SerializeField] private Transform m_contentParent;
        [SerializeField] private Button m_loadButton;
        [SerializeField] private TMP_Text m_description;
        [SerializeField] private SceneEntryData[] m_entries;
        private SceneEntryData m_activeData;
        private void Awake() {
            foreach (var entryData in m_entries)
            {
                var instance = Instantiate(m_entryPrefab, m_contentParent);
                var entry = instance.GetComponent<UISceneEntry>();
                entry.Icon.sprite = entryData.Image;
                entry.Title.text = entryData.Title;
                var entryButton = instance.GetComponent<Button>();
                entryButton.onClick.AddListener(() => {HandleSelected(entryData);});
            }
            m_description.text = m_activeData == null ? "No Scene selected." : m_description.text;
            m_loadButton.interactable = m_activeData != null;
            m_loadButton.onClick.AddListener(HandleLoad);
        }

        private void HandleLoad()
        {
            if(m_activeData == null) return;
            SceneManager.LoadScene(m_activeData.SceneIndex, LoadSceneMode.Single);
        }

        public void HandleSelected(SceneEntryData data)
        {
            m_activeData = data;
            m_description.text = m_activeData == null ? "No Scene selected." : data.Description;
            m_loadButton.interactable = m_activeData != null;
        }
    }
}