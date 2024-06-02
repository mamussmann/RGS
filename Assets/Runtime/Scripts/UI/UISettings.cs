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
using UnityEngine;
using TMPro;
namespace RGS.UI
{
    public class UISettings : MonoBehaviour
    {
        public string SelectedPythonPath => m_selectedPythonPath;
        [SerializeField] private TMP_InputField m_textField;
        private string m_selectedPythonPath;
        private bool m_isFullscreen;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            // assume the default installation path or registered env var
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || PLATFORM_STANDALONE_LINUX
            m_selectedPythonPath = "/usr/bin/python3";
#else
            m_selectedPythonPath = "python3";
#endif
            m_textField.text = m_selectedPythonPath;
            m_textField.textComponent.SetText(m_selectedPythonPath);
            m_uiMediator.OnEventToggleClicked.AddListener(HandleToggleClicked);
            m_textField.onEndEdit.AddListener(HandleTextFieldSubmit);
            Application.targetFrameRate = -1;
        }
        private void Start() {
            m_uiMediator.OnEventToggleClicked.Invoke(ToggleEventType.TOGGLE_WINDOWED, Screen.fullScreen);
        }

        private void HandleTextFieldSubmit(string text)
        {
            m_selectedPythonPath = text;
            m_textField.text = text;
            m_textField.textComponent.SetText(m_selectedPythonPath);
        }

        private void HandleToggleClicked(ToggleEventType eventType, bool value)
        {
            if(eventType == ToggleEventType.TOGGLE_WINDOWED){
                m_isFullscreen = value;
                Screen.fullScreenMode = m_isFullscreen ? FullScreenMode.FullScreenWindow :  FullScreenMode.Windowed;
                Screen.fullScreen = m_isFullscreen;
                if(m_isFullscreen) {
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, m_isFullscreen, -1);
                }
            }
        }

        private void OnDestroy() {
            m_uiMediator.OnEventToggleClicked.RemoveListener(HandleToggleClicked);
            m_textField.onEndEdit.RemoveListener(HandleTextFieldSubmit);
        }

    }
}