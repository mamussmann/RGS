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
using UnityEngine.UI;

namespace RGS.UI
{

    public class UITimeControlPanel : MonoBehaviour
    {
        [SerializeField] private Button m_pauseButton;
        [SerializeField] private Image m_pauseButtonIcon;
        [SerializeField] private Button m_playButton;
        [SerializeField] private Image m_playButtonIcon;
        [SerializeField] private Button m_fastForwardButton;
        [SerializeField] private Image m_fastForwardButtonIcon;
        private int m_speedMode;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() 
        {
            m_pauseButton.onClick.AddListener(HandlePauseClicked);
            m_playButton.onClick.AddListener(HandlePlayClicked);
            m_fastForwardButton.onClick.AddListener(HandleFastForwardClicked);
            m_speedMode = 0;
            m_playButtonIcon.color = Color.black;
            m_pauseButtonIcon.color = Color.gray;
            m_fastForwardButtonIcon.color = Color.black;
            m_playButton.interactable = true;
            m_pauseButton.interactable = false;
            m_fastForwardButton.interactable = true;
        }
        private void Update() {
            if(!SessionInfo.IsInputEnabled) return;
            if (Input.GetKeyUp(KeyCode.Plus) || Input.GetKeyUp(KeyCode.KeypadPlus))
            {
                if (m_speedMode == 0)
                {
                    HandlePlayClicked();
                }
                else if (m_speedMode == 1)
                {
                    HandleFastForwardClicked();
                }
            }
            else if (Input.GetKeyUp(KeyCode.Minus) || Input.GetKeyUp(KeyCode.KeypadMinus))
            {
                if (m_speedMode == 1)
                {
                    HandlePauseClicked();
                }
                else if (m_speedMode == 2)
                {
                    HandlePlayClicked();
                }
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                if (m_speedMode == 0)
                {
                    HandlePlayClicked();
                }
                else
                {
                    HandlePauseClicked();
                }
            }
        }
        private void HandlePauseClicked()
        {
            m_uiMediator.OnPauseEvent.Invoke();
            m_playButtonIcon.color = Color.black;
            m_pauseButtonIcon.color = Color.gray;
            m_fastForwardButtonIcon.color = Color.black;
            m_playButton.interactable = true;
            m_pauseButton.interactable = false;
            m_fastForwardButton.interactable = true;
            m_speedMode = 0;
        }
        private void HandlePlayClicked()
        {
            m_uiMediator.OnPlayEvent.Invoke();
            m_playButtonIcon.color = Color.gray;
            m_pauseButtonIcon.color = Color.black;
            m_fastForwardButtonIcon.color = Color.black;
            m_playButton.interactable = false;
            m_pauseButton.interactable = true;
            m_fastForwardButton.interactable = true;
            m_speedMode = 1;
        }
        private void HandleFastForwardClicked()
        {
            m_uiMediator.OnFastForwardEvent.Invoke();
            m_playButtonIcon.color = Color.black;
            m_pauseButtonIcon.color = Color.black;
            m_fastForwardButtonIcon.color = Color.gray;
            m_playButton.interactable = true;
            m_pauseButton.interactable = true;
            m_fastForwardButton.interactable = false;
            m_speedMode = 2;
        }
        private void OnDestroy() 
        {
            m_pauseButton.onClick.RemoveAllListeners();
            m_playButton.onClick.RemoveAllListeners();
            m_fastForwardButton.onClick.RemoveAllListeners();
        }
    }

}
