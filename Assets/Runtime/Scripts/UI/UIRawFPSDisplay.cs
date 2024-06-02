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
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace RGS.UI
{

    [RequireComponent(typeof(TMP_Text))]
    public class UIRawFPSDisplay : MonoBehaviour
    {
        private TMP_Text m_fpsText;
        private List<float> m_fpsBuffer = new List<float>();
        private float m_timeCounter;
        private void Awake() {
            m_fpsText = GetComponent<TMP_Text>();
            m_timeCounter = 0.0f;
        }
        void Update()
        {
            if (m_timeCounter <= 1.0f)
            {
                m_timeCounter += Time.smoothDeltaTime;
                m_fpsBuffer.Add(1.0f / Time.smoothDeltaTime);
            }
            else
            {
                float avgFps = m_fpsBuffer.Sum() / m_fpsBuffer.Count;
                m_fpsBuffer = new List<float> { 1.0f / Time.smoothDeltaTime };
                m_fpsText.text = $"FPS: {(int)(avgFps)}";
                m_timeCounter = 0.0f;
            }
        }
    }

}