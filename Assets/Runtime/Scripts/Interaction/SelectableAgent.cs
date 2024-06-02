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
using RGS.Models;
using RGS.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RGS.Interaction
{

    public class SelectableAgent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private MeshRenderer m_meshRenderer;
        [SerializeField] private float m_scale;
        private long m_uniqueAgentId;
        private AgentInfoPanelModel m_dataModel;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Start() {
            Physics.queriesHitTriggers = true;
            transform.localScale = Vector3.one * m_scale;
        }
        public void SetAgentInformation(long uniqueAgentId, Color color, Vector3 position, Vector3 direction, AgentInfoPanelModel model)
        {
            m_uniqueAgentId = uniqueAgentId;
            m_meshRenderer.material.color = color;
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            m_dataModel = model;
        }
        public void OnPointerEnter(PointerEventData pointerEventData)
        {
                transform.localScale = Vector3.one * (m_scale * 2.0f);
        }
        public void OnPointerExit(PointerEventData pointerEventData)
        {
            transform.localScale = Vector3.one * m_scale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_uiMediator.OnAgentSelected.Invoke(m_uniqueAgentId, transform.position, m_dataModel);
        }

    }

}
