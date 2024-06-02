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
using PCMTool.Tree;
using RGS.Configuration.UI;
using RGS.Configurations;
using RGS.Configurations.Root;
using RGS.Extension;
using RGS.Interaction;
using RGS.Models;
using RGS.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RGS.Simulation
{
    [Serializable]
    public struct PlantPositionData
    {
        public PlantConfiguration Config;
        public Vector3 Position;
    }
    public class SimulationInteractor : MonoBehaviour
    {
        [SerializeField] private Transform m_hitTarget;
        [SerializeField] private Camera m_camera;
        [SerializeField] private LeafRenderer m_leafRenderer;
        [SerializeField] private RuntimeLeafModifier m_leafModifier;
        [SerializeField] private RootGrowthSimulationArea m_simulationArea;
        [SerializeField] private PlantConfiguration[] m_plantConfigurations;
        [SerializeField] private float m_waterSourceHeight;
        [SerializeField] private Color m_soilColorMin;
        [SerializeField] private Color m_soilColorMax;
        [SerializeField] private Color m_obstacleColorMin;
        [SerializeField] private Color m_obstacleColorMax;
        [SerializeField] private LineRenderer m_lineRenderer;
        [SerializeField] private Transform m_magneticField;
        [SerializeField] private WaterSource m_waterSource;
        [SerializeField] private PlantPositionData[] m_initialPlants;
        private Vector3 m_obstacleStart, m_obstacleEnd;
        private Vector3 m_soilMinColor => new Vector3(m_soilColorMin.r, m_soilColorMin.g, m_soilColorMin.b);
        private Vector3 m_soilMaxColor => new Vector3(m_soilColorMax.r, m_soilColorMax.g, m_soilColorMax.b);
        private Vector3 m_obstacleMinColor => new Vector3(m_obstacleColorMin.r, m_obstacleColorMin.g, m_obstacleColorMin.b);
        private Vector3 m_obstacleMaxColor => new Vector3(m_obstacleColorMax.r, m_obstacleColorMax.g, m_obstacleColorMax.b);
        private PieMenuActionType m_currentInteractionAction;
        private int m_currentActionIndex;
        private bool m_isPaused = true;
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_hitTarget.gameObject.SetActive(false);
            m_currentInteractionAction = PieMenuActionType.GO_BACK;
            m_lineRenderer.transform.position = Vector3.zero; 
            m_lineRenderer.enabled = false;
            m_magneticField.gameObject.SetActive(false);
        }
        private void Start() {
            m_interactionMediator.OnPieActionSelected.AddListener(HandlePieActionSelected);
            m_uiMediator.OnPauseEvent.AddListener(() => {m_isPaused = true;});
            m_uiMediator.OnPlayEvent.AddListener(() => {m_isPaused = false; m_waterSource?.ShowPreview(false);});
            m_uiMediator.OnFastForwardEvent.AddListener(() => {m_isPaused = false;m_waterSource?.ShowPreview(false);});
            foreach (var plant in m_initialPlants)
            {
                var model = m_simulationArea.PlaceSeed(plant.Config, plant.Position);
                m_interactionMediator.OnNewSeedAdded.Invoke(model);
            }
        }
        private void HandlePieActionSelected(PieMenuActionType pieMenuActionType, int index)
        {
            m_currentInteractionAction = pieMenuActionType;
            m_currentActionIndex = index;
            m_hitTarget.gameObject.SetActive(false);
            if(pieMenuActionType == PieMenuActionType.SET_RENDERING && index == 0)
            {
                m_leafRenderer.ShowAllPoints = false;
            }else if(pieMenuActionType == PieMenuActionType.SET_RENDERING && index == 1)
            {
                m_leafRenderer.ShowAllPoints = true;
            }else if(pieMenuActionType == PieMenuActionType.PLACE_ELEMENT)
            {
                m_uiMediator.OnShowHeatmap.Invoke(index);
            }
            m_waterSource?.ShowPreview(pieMenuActionType == PieMenuActionType.MOVE_WATER);
        }
        private void Update() {
            if (!m_isPaused) return;
            if (m_currentInteractionAction == PieMenuActionType.PLANT_SEED)
            {
                UpdatePlacement(hitPosition => {
                    int configIndex = Mathf.Clamp(m_currentActionIndex, 0, m_plantConfigurations.Length);
                    var model = m_simulationArea.PlaceSeed(m_plantConfigurations[configIndex], hitPosition);
                    m_interactionMediator.OnNewSeedAdded.Invoke(model);
                });
            }else if (m_currentInteractionAction == PieMenuActionType.PLACE_OBSTACLE && m_currentActionIndex == 1 && m_leafRenderer.EnableCullingPlane)
            {
                UpdatePlacement(HandleObstaclePlaceStart,HandleObstaclePlaceUpdate, HandleObstaclePlaceEnd);
            }else if (m_currentInteractionAction == PieMenuActionType.PLACE_ELEMENT)
            {
                UpdatePlacement(hitPosition => {
                    int collisionPointId = m_currentActionIndex;
                    m_leafModifier.AddSphere(hitPosition, 0.025f, 500, m_soilMinColor, m_soilMaxColor, collisionPointId, 0.01f, 1.0f);
                });
            }else if (m_currentInteractionAction == PieMenuActionType.MOVE_MAGNET && m_leafRenderer.EnableCullingPlane)
            {
                UpdatePlacement(HandleMoveMagnetStart,HandleMoveMagnetUpdate, HandleMoveMagnetEnd);
            }else if (m_currentInteractionAction == PieMenuActionType.MOVE_WATER && !m_leafRenderer.EnableCullingPlane)
            {
                UpdatePlacement(HandleMoveWaterStart,HandleMoveWaterUpdate, HandleMoveWaterEnd);
            }
        }

        private void HandleMoveWaterStart(Vector3 hitPosition)
        {
            m_obstacleStart = hitPosition;
            m_obstacleStart.y = m_waterSourceHeight;
            m_waterSource?.UpdateScale(m_obstacleStart, m_obstacleStart);
        }
        private void HandleMoveWaterUpdate(Vector3 hitPosition)
        {
            m_obstacleEnd = hitPosition;
            m_obstacleEnd.y = m_waterSourceHeight;
            m_waterSource?.UpdateScale(m_obstacleStart, m_obstacleEnd);
        }
        private void HandleMoveWaterEnd(Vector3 hitPosition)
        {
            m_obstacleEnd = hitPosition;
            m_obstacleEnd.y = m_waterSourceHeight;
            m_waterSource?.UpdateScale(m_obstacleStart, m_obstacleEnd);
        }

        private void HandleMoveMagnetStart(Vector3 hitPosition)
        {
            m_obstacleStart = hitPosition; 
            m_magneticField.gameObject.SetActive(true);
            m_magneticField.position = hitPosition;
        }
        private void HandleMoveMagnetUpdate(Vector3 hitPosition)
        {
            m_obstacleEnd = hitPosition;
            m_magneticField.rotation = Quaternion.LookRotation(m_obstacleEnd - m_obstacleStart);
            m_magneticField.localScale = Vector3.one * (Vector3.Distance(m_obstacleStart, m_obstacleEnd)) * 100.0f;
        }
        private void HandleMoveMagnetEnd(Vector3 hitPosition)
        {
            m_obstacleEnd = hitPosition;
            m_magneticField.rotation = Quaternion.LookRotation(m_obstacleEnd - m_obstacleStart);
            m_magneticField.localScale = Vector3.one * (Vector3.Distance(m_obstacleStart, m_obstacleEnd)) * 100.0f;
            m_interactionMediator.OnMagneticFieldUpdate.Invoke();
        }
        private void HandleObstaclePlaceStart(Vector3 hitPosition)
        {
            m_obstacleStart = hitPosition; 
            m_lineRenderer.enabled = true;
            m_lineRenderer.SetPositions(new Vector3[]{m_obstacleStart, m_obstacleEnd});
        }
        private void HandleObstaclePlaceUpdate(Vector3 hitPosition)
        {
            m_obstacleEnd = hitPosition;
            m_lineRenderer.SetPositions(new Vector3[]{m_obstacleStart, m_obstacleEnd});
        }
        private void HandleObstaclePlaceEnd(Vector3 hitPosition)
        {
            m_obstacleEnd = hitPosition;
            m_lineRenderer.enabled = false;
            float length = Vector3.Distance(m_obstacleStart, m_obstacleEnd);
            Vector3 direction = m_leafRenderer.PlaneDirection.normalized * length;
            Vector3 dir2 = m_obstacleEnd - m_obstacleStart;
            Vector3 upDir = Vector3.Cross(direction, dir2).normalized * 0.01f;
            Vector3 origin = m_obstacleStart - (direction * 0.5f) - (upDir * 0.5f);
            int pointCount = (int)((length / 0.1f) * 10000.0f);
            //Debug.Log("pointCount:"+pointCount);
            int obstaclePointType = RGSConfiguration.Get().GetIndexOfPointType("Obstacle");
            m_leafModifier.AddCube(origin, direction, dir2, upDir, pointCount, m_obstacleMinColor, m_obstacleMaxColor, obstaclePointType, 0.005f, 1.0f);
        }

        private void UpdatePlacement(Action<Vector3> hitPlacementFunctionMouseDown, Action<Vector3> hitPlacementFunctionMouse = null, Action<Vector3> hitPlacementFunctionMouseUp = null)
        {
            Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
            Vector3 hitPosition;
            m_hitTarget.gameObject.SetActive(false);
            if(m_leafRenderer.EnableCullingPlane) {
                if (m_leafModifier.HitPCWithCullingPlane(ray.origin, ray.direction * 0.1f, 0.01f, out hitPosition, m_leafRenderer.PlanePosition, m_leafRenderer.PlaneDirection))
                {
                    m_hitTarget.position = hitPosition;
                    m_hitTarget.gameObject.SetActive(true);
                    if(Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && hitPlacementFunctionMouseDown != null)
                    {
                        hitPlacementFunctionMouseDown(hitPosition);
                    }else if(Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject() && hitPlacementFunctionMouse != null)
                    {
                        hitPlacementFunctionMouse(hitPosition);
                    }else if(Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject() && hitPlacementFunctionMouseUp != null)
                    {
                        hitPlacementFunctionMouseUp(hitPosition);
                    }
                }
            }else{
                if (m_leafModifier.HitPC(ray.origin, ray.direction * 0.1f, 0.01f, out hitPosition))
                {
                    m_hitTarget.position = hitPosition;
                    m_hitTarget.gameObject.SetActive(true);
                    if(Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && hitPlacementFunctionMouseDown != null)
                    {
                        hitPlacementFunctionMouseDown(hitPosition);
                    }else if(Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject() && hitPlacementFunctionMouse != null)
                    {
                        hitPlacementFunctionMouse(hitPosition);
                    }else if(Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject() && hitPlacementFunctionMouseUp != null)
                    {
                        hitPlacementFunctionMouseUp(hitPosition);
                    }
                }
            }
        }
        
        private void OnDestroy() {
            m_interactionMediator.OnPieActionSelected.RemoveListener(HandlePieActionSelected);
        }
    }
}