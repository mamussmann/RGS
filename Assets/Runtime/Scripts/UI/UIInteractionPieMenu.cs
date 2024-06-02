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
using System.Collections.Generic;
using RGS.Configuration.UI;
using RGS.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{

    public class UIInteractionPieMenu : MonoBehaviour
    {
        [SerializeField] private GameObject m_pieActionItemPrefab;
        [SerializeField] private RectTransform m_pieContainer;
        [SerializeField] private Image m_selectionRingImage;
        [SerializeField] private PieMenuContent[] m_pieMenuContent;
        private List<UIPieActionItem> m_pieActionItemPool;
        [SerializeField] private float m_centerMinSelectDistance = 50.0f;
        [SerializeField] private float m_centerDistance = 100.0f;
        [SerializeField] private float m_layerDistance = 75.0f;
        private int[] m_currentSelectedIndex;
        private PieMenuContent[] m_selectedContent;
        private Material m_selectionRingMaterial;
        private bool m_isPaused = true;
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_pieActionItemPool = new List<UIPieActionItem>();
            m_currentSelectedIndex = new int[16];
            m_selectedContent = new PieMenuContent[16];
            m_selectionRingMaterial = m_selectionRingImage.material;
            HideMenu();
            ShowMenuConfiguration(0);
            m_interactionMediator.OnPieActionSelected.AddListener(HandlePieActionSelected);
            m_uiMediator.OnPauseEvent.AddListener(() => {m_isPaused = true; Reset();});
            m_uiMediator.OnPlayEvent.AddListener(() => {m_isPaused = false; Reset();});
            m_uiMediator.OnFastForwardEvent.AddListener(() => {m_isPaused = false; Reset();});
        }
        
        private void Reset()
        {
            m_interactionMediator.OnPieActionSelected.Invoke(PieMenuActionType.GO_BACK, 0);
        }
        private void HandlePieActionSelected(PieMenuActionType type, int index)
        {
        }
        private void ShowMenuConfiguration(int layerIndex)
        {
            
            foreach (var actionItem in m_pieActionItemPool)
            {
                actionItem.gameObject.SetActive(false);
            }
            int poolIndex = 0;
            for (int j = 0; j < m_selectedContent.Length; j++)
            {
                if(j < m_selectedContent.Length - 1 && m_selectedContent[j + 1] == null && layerIndex >= j+1) 
                {
                    layerIndex = j;
                }
                if(m_selectedContent[j] == null) break;
                int pieIndex = 0;
                PieMenuContent currentContent = m_selectedContent[j];
                for (int i = 0; i < currentContent.AvailableActions.Length; i++)
                {
                    if (poolIndex >= m_pieActionItemPool.Count) {
                        var instance = GameObject.Instantiate(m_pieActionItemPrefab, m_pieContainer);
                        m_pieActionItemPool.Add(instance.GetComponent<UIPieActionItem>());
                    }
                    float angle = GetAngle(j, pieIndex);
                    m_pieActionItemPool[poolIndex].gameObject.transform.localPosition = Quaternion.Euler(0.0f, 0.0f, -Mathf.Rad2Deg * angle) * (Vector3.up * ((j+1) * m_centerDistance));
                    m_pieActionItemPool[poolIndex].SetContent(currentContent.AvailableActions[i].ActionName, currentContent.AvailableActions[i].ActionIcon);
                    m_pieActionItemPool[poolIndex].SetIsFocusLayer(j == layerIndex);
                    m_pieActionItemPool[poolIndex].SetIsPath(j < layerIndex && i == m_currentSelectedIndex[j]);
                    m_pieActionItemPool[poolIndex].IsGoToIndex(currentContent.AvailableActions[i].ActionType == PieMenuActionType.GO_TO_INDEX, angle);
                    if(i == GetSelectionIndex(layerIndex) && j == layerIndex)
                    {
                        m_pieActionItemPool[poolIndex].ShowHightlight();
                    }
                    m_pieActionItemPool[poolIndex].SetInteractable(m_isPaused || currentContent.AvailableActions[i].IsAvailableInPlayMode);
                    m_pieActionItemPool[poolIndex].gameObject.SetActive(true);
                    pieIndex++;
                    poolIndex++;
                }
            }
        }

        private float GetAngleDeltaRad(int layer)
        {
            float deltaAngle = Mathf.Deg2Rad * GetDeltaAngleDeg();
            if(layer <= 0) return deltaAngle;
            return  (deltaAngle * (Mathf.Pow(0.5f, (layer))));
        }

        private float GetAngle(int layer, int pieIndex, bool offsetByHalfLayerDelta = true)
        {
            float deltaAngle = Mathf.Deg2Rad * GetDeltaAngleDeg();
            float layerDelta = GetAngleDeltaRad(layer);
            float previousDelta = GetAngleDeltaRad(layer - 1);
            int previousIndex = layer == 0 ? 0 : m_currentSelectedIndex[layer - 1];
            float previousAngle = layer > 0 ? GetAngle(layer-1, m_currentSelectedIndex[layer - 1]) : 0.0f;
            float angle = (previousAngle + (pieIndex * layerDelta));
            if(offsetByHalfLayerDelta){
                angle += (layerDelta* 0.5f);
            }
            if(layer > 0)
            {
                angle -= ((int)((m_selectedContent[layer].AvailableActions.Length) / 2.0f) * layerDelta);
            }
            return angle;
        }
        private void Update() {
            if(!SessionInfo.IsInputEnabled) return;
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F)) {
                ShowMenu();
                m_interactionMediator.OnPieActionSelected.Invoke(PieMenuActionType.GO_BACK, 0);
            }
            if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.F)) {
                UpdateIndices();
                UpdateContent();
                ShowMenuConfiguration(GetLayerIndex());

                int selectionIndex = GetSelectionIndex(GetLayerIndex());
                if (selectionIndex != -1) {
                    m_selectionRingMaterial.SetFloat("_angleValue", (Mathf.PI * 2.0f) -  (Mathf.Deg2Rad * GetMouseAngleDeg()));
                    m_selectionRingMaterial.SetFloat("_angleDelta", Mathf.Deg2Rad * GetDeltaAngleDeg());
                    m_selectionRingMaterial.SetFloat("_isNoSelect", 0.0f);
                }else {
                    m_selectionRingMaterial.SetFloat("_isNoSelect", 1.0f);
                }
            }

            if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.F)) {
                UpdateIndices();
                UpdateContent();
                int layer = Mathf.Min(GetLayerIndex(), GetMaxPossibleLayer());
                int selectionIndex = GetSelectionIndex(layer);
                if (selectionIndex != -1 && layer != -1 && selectionIndex < m_selectedContent[layer].AvailableActions.Length) {
                    var action = m_selectedContent[layer].AvailableActions[selectionIndex];
                    if(m_isPaused || action.IsAvailableInPlayMode) {
                        m_interactionMediator.OnPieActionSelected.Invoke(action.ActionType, action.ActionIndex);
                    }
                }
                HideMenu();
            }
        }
        private int GetMaxPossibleLayer()
        {
            int lastLayer = 0;
            for (int i = 0; i < m_selectedContent.Length; i++)
            {
                if(m_selectedContent[i] == null)
                {
                    return lastLayer;
                }
                lastLayer = i;
            }
            return 0;
        }
        private void UpdateContent()
        {
            for (int i = 0; i < m_selectedContent.Length; i++)
            {
                m_selectedContent[i] = null;
            }
            m_selectedContent[0] = m_pieMenuContent[0];
            for (int i = 0; i < m_currentSelectedIndex.Length - 1; i++)
            {
                if(m_currentSelectedIndex[i] < 0 || m_currentSelectedIndex[i] >= m_selectedContent[i].AvailableActions.Length) break;
                var action = m_selectedContent[i].AvailableActions[m_currentSelectedIndex[i]];
                if(action.ActionType == PieMenuActionType.GO_TO_INDEX)
                {
                    m_selectedContent[i+1] = m_pieMenuContent[action.ActionIndex];
                }else {
                    break;
                }
            }
        }
        private void UpdateIndices()
        {
            int layerIndex = Mathf.Max(GetLayerIndex(), 0);
            for (int i = layerIndex; i < m_currentSelectedIndex.Length; i++)
            {
                m_currentSelectedIndex[i] = -1;
            }
            m_currentSelectedIndex[layerIndex] = GetSelectionIndex(layerIndex);
            //string log = "";
            //for (int i = 0; i < m_currentSelectedIndex.Length; i++)
            //{
            //    log += m_currentSelectedIndex[i]+",";
            //}
            //Debug.Log(log);
        }
        private int GetLayerIndex()
        {
            float centerDistance = Vector3.Distance(Input.mousePosition, m_pieContainer.position);
            if (centerDistance < m_centerMinSelectDistance) return -1;
            return (int)((centerDistance - m_centerMinSelectDistance) / m_layerDistance);
        }
        private int GetSelectionIndex(int activeLayer)
        {
            if(activeLayer == -1) return -1;
            if(m_selectedContent[activeLayer] == null)
            {
                for (int i = 0; i < m_selectedContent.Length; i++)
                {
                    if(m_selectedContent[i] == null)
                    {
                        break;
                    }
                    activeLayer = i;
                }
            }
            float centerDistance = Vector3.Distance(Input.mousePosition, m_pieContainer.position);
            if (centerDistance < m_centerMinSelectDistance) return -1;

            float deltaAngle = Mathf.Rad2Deg * GetAngleDeltaRad(activeLayer);
            float angle = GetMouseAngleDeg();
            float startAngle = Mathf.Rad2Deg * GetAngle(activeLayer, 0, false);
            float endAngle = Mathf.Rad2Deg * GetAngle(activeLayer, m_selectedContent[activeLayer].AvailableActions.Length -1, true);
            angle = startAngle < 0  && angle < 180.0f? angle + Mathf.Abs(startAngle) : angle;
            angle = startAngle < 0 && angle > 180.0f ? angle - (360 + startAngle) : angle;
            //Debug.Log($"angle: {angle}");
            angle = endAngle > 360  && angle < 180.0f? angle + 360.0f : angle;
            angle = startAngle > 0 ? angle - startAngle : angle;
            //Debug.Log($"StartAngle: {startAngle}, endAngle {endAngle}, angle {angle}, deltaAngle {deltaAngle} index: {(int) (angle / deltaAngle)}");
            if(angle < 0) return -1;
            return (int) (angle / deltaAngle);
        }
        private float GetDeltaAngleDeg()
        {
            float deltaAngle = (Mathf.PI * 2.0f) / m_pieMenuContent[0].AvailableActions.Length;
            return  Mathf.Rad2Deg * deltaAngle;
        }
        private float GetMouseAngleDeg()
        {
            float angle = Vector3.Angle((Input.mousePosition - m_pieContainer.position).normalized, Vector3.up);
            angle =  Vector3.Dot((Input.mousePosition - m_pieContainer.position).normalized, Vector3.right) > 0.0f? angle : 360.0f - angle;
            return angle;
        }
        public void ShowMenu()
        {
            m_pieContainer.gameObject.SetActive(true);
            m_pieContainer.position = Input.mousePosition;
        }

        public void HideMenu()
        {
            m_pieContainer.gameObject.SetActive(false);
        }
    }

}
