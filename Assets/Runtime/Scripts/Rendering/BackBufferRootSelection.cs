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
using RGS.Configuration.UI;
using RGS.Interaction;
using RGS.Models;
using RGS.UI;
using UnityEngine;

public class BackBufferRootSelection : MonoBehaviour
{
    [SerializeField] private RenderTexture m_rootSelectRenderTexture;
    [SerializeField] private Camera m_bufferRenderingCamera;
    private Texture2D m_rootSelectionTexture;
    private bool m_isInRootPointSelection;
    private PlantSeedModel m_activeSeedModel;
    private Vector3 m_cuttingPosition;
    private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
    private readonly UIMediator m_uiMediator = UIMediator.Get();
    private void Awake() 
    {    
        m_rootSelectionTexture = new Texture2D(m_rootSelectRenderTexture.width, m_rootSelectRenderTexture.height, TextureFormat.RFloat,0, true);
        m_interactionMediator.OnPieActionSelected.AddListener(HandlePieActionSelected);
        m_uiMediator.OnSelectionChanged.AddListener(HandleSelectionChanged);
        m_bufferRenderingCamera.enabled = false;
        m_uiMediator.OnScreenResolutionChanged.AddListener(HandleScreenSizeChange);
    }

    private void HandleScreenSizeChange()
    {
        m_rootSelectRenderTexture.Release();
        m_rootSelectRenderTexture.width = (int) (Screen.width * 0.5f);
        m_rootSelectRenderTexture.height = (int) (Screen.height * 0.5f);
        m_rootSelectRenderTexture.Create();
        m_rootSelectionTexture = new Texture2D(m_rootSelectRenderTexture.width, m_rootSelectRenderTexture.height, TextureFormat.RFloat,0, true);
        //Debug.Log($"RT resize to {m_rootSelectRenderTexture.width}, {m_rootSelectRenderTexture.height}");
    }

    private void HandlePieActionSelected(PieMenuActionType menuActionType, int index)
    {
        if(menuActionType == PieMenuActionType.SELECT_MODE && index == 3 && m_activeSeedModel != null)
        {
            m_isInRootPointSelection = true;
            m_activeSeedModel.UpdateRootPointSelectionBuffer();
            m_bufferRenderingCamera.enabled = true;
        }else {
            m_isInRootPointSelection = false;   
            m_bufferRenderingCamera.enabled = false;
        }
        HighlightRootPointCutRendererFeature.Instance.SetActive(m_isInRootPointSelection);
    }
    private void HandleSelectionChanged(Guid guid)
    {
        PlantSeedModel[] seedModels = GameObject.FindObjectsOfType<PlantSeedModel>();
        foreach (var seedModel in seedModels)
        {
            if(seedModel.Identifier.Equals(guid))
            {
                m_activeSeedModel = seedModel;
            }
        }
    }

    void Update()
    {
        if(m_isInRootPointSelection)
        {
            ReadSelectRootRT();
        }
    }

    private void ReadSelectRootRT()
    {

        Vector3 mousePos = Input.mousePosition;
        mousePos.x = mousePos.x / (float)Screen.width;
        mousePos.y = mousePos.y / (float)Screen.height;
        int pixelX = (int)(mousePos.x * m_rootSelectionTexture.width);
        int pixelY = m_rootSelectionTexture.height - (int)(mousePos.y * m_rootSelectionTexture.height);
        pixelX = Mathf.Clamp(pixelX, 0, m_rootSelectionTexture.width -1);
        pixelY = Mathf.Clamp(pixelY, 0, m_rootSelectionTexture.height -1);
        var lastActive = RenderTexture.active;
        RenderTexture.active = m_rootSelectRenderTexture;
        m_rootSelectionTexture.ReadPixels(new Rect(pixelX, pixelY, 1, 1), pixelX, pixelY);
        m_rootSelectionTexture.Apply();
        RenderTexture.active = lastActive;
        Color col = m_rootSelectionTexture.GetPixel(pixelX, pixelY);
        int currentCuttingIndex = Mathf.RoundToInt(col.r) - 1;
        //Debug.Log($"cuttingIndex,{cuttingIndex}, raw:{col.r}");
        if(currentCuttingIndex == -1) {
            HighlightRootPointCutRendererFeature.Instance.SetActive(false);
            return;
        }
        var cuttingData = m_activeSeedModel.GetCuttingPoint(currentCuttingIndex);
        m_cuttingPosition = cuttingData.Position;

        if(Input.GetMouseButtonDown(0))
        {
            m_interactionMediator.OnCutRootAt.Invoke(cuttingData.AgentUniqueId, cuttingData.TimeStamp, m_activeSeedModel);
        }else {
            m_interactionMediator.OnPreviewCutRootAt.Invoke(cuttingData.AgentUniqueId, cuttingData.TimeStamp, m_activeSeedModel);
        }
    }

    private void OnDestroy() {        
        m_interactionMediator.OnPieActionSelected.RemoveListener(HandlePieActionSelected);
        m_uiMediator.OnSelectionChanged.RemoveListener(HandleSelectionChanged);
        m_uiMediator.OnScreenResolutionChanged.RemoveListener(HandleScreenSizeChange);
    }
}
