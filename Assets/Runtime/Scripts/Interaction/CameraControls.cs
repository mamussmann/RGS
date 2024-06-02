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
using RGS.Models;
using RGS.UI;
using Unity.Mathematics;
using UnityEngine;

namespace RGS.Interaction
{
    public class CameraControls : MonoBehaviour
    {
        [SerializeField] private LeafRenderer m_leafRenderer;
        [SerializeField] private float m_moveSpeed;
        [SerializeField] private float m_minFocus;
        [SerializeField] private float m_maxFocus;
        [SerializeField] private float m_scrollSpeed;
        [SerializeField] private float m_topViewPitch;
        //
        [SerializeField] private float m_transitionTime = 0.25f;
        [SerializeField] private float m_topViewTurnSpeed;
        [SerializeField] private Vector2 m_sensitivity;
        //
        private PlantSeedModel m_selecedPlant;
        private CameraStates m_currentState = CameraStates.DEFAULT;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private Quaternion m_targetRotation;
        private float m_focusDistance;
        private bool m_onTransisionFinished;
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private float m_yaw;
        private float m_pitch;
        private Vector3 m_lastMousePosition;
        private bool m_isCullInCamPlane = false;
        private void Awake() {
            m_uiMediator.OnEventToggleClicked.AddListener(HandleEventToggleClicked);
        }

        private void HandleEventToggleClicked(ToggleEventType toggleEventType, bool value)
        {
            if (toggleEventType == ToggleEventType.TOGGLE_CULL_CAM_PLANE) {
                m_isCullInCamPlane = value;
            }
        }

        private void Start() {
            m_uiMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
            m_uiMediator.OnSelectionChanged.AddListener(HandleSelectionChanged);
            m_interactionMediator.OnPieActionSelected.AddListener(HandlePieActionSelected);
            m_targetRotation = Quaternion.Euler(m_topViewPitch, 0.0f, 0.0f);
            m_onTransisionFinished = true;
            m_currentState = CameraStates.DEFAULT;
        }

        private void HandlePieActionSelected(PieMenuActionType pieMenuActionType, int index)
        {
            if(pieMenuActionType == PieMenuActionType.CAMERA_VIEW && index == 0)
            {
                m_currentState = CameraStates.DEFAULT;
                HandleCameraState();
            }
            else if(pieMenuActionType == PieMenuActionType.CAMERA_VIEW && index == 1 && m_selecedPlant != null)
            {
                m_currentState = CameraStates.PLANT_FOCUS;
                HandleCameraState();
            }
        }
        private void HandleSelectionChanged(Guid plantGuid)
        {
            var seedModels = GameObject.FindObjectsOfType<PlantSeedModel>();
            foreach (var seedModel in seedModels)
            {
                if (seedModel.Identifier.Equals(plantGuid))
                {
                    m_selecedPlant = seedModel;
                    HandleCameraState();
                }
            }
        }

        private void HandleEventButtonClicked(ButtonEventType buttonEventType)
        {
            if(buttonEventType == ButtonEventType.FOCUS_PLANT && m_selecedPlant != null) {
                m_currentState = CameraStates.PLANT_FOCUS;
                HandleCameraState();
            }
        }
        private void HandleCameraState()
        {
            switch (m_currentState)
            {
                case CameraStates.DEFAULT:
                    m_onTransisionFinished = false;
                    m_pitch = m_topViewPitch;
                    Vector3 target = transform.position;
                    target.y = 1.0f;
                    m_targetRotation = Quaternion.Euler(m_topViewPitch, m_yaw, 0.0f);
                    LeanTween.rotate(gameObject, m_targetRotation.eulerAngles, m_transitionTime);
                    LeanTween.move(gameObject, target, m_transitionTime).setOnComplete(_ => m_onTransisionFinished = true);
                    break;
                case CameraStates.PLANT_FOCUS:
                    m_onTransisionFinished = false;
                    m_targetRotation = Quaternion.identity;
                    m_focusDistance = -0.5f;
                    Vector3 position = m_selecedPlant.transform.position;
                    position.y = m_selecedPlant.BoundingBox.center.y;
                    SetCullingPlane();
                    m_leafRenderer.EnableCullingPlane = true;
                    LeanTween.move(gameObject, CalculateOrbitalPosition(), m_transitionTime).setOnComplete(_ => m_onTransisionFinished = true);
                    break;
            }
        }
        private void Update() {
            if(!SessionInfo.IsInputEnabled) return;
            if(!m_onTransisionFinished) return;
            switch (m_currentState)
            {
                case CameraStates.DEFAULT:
                    DefaultMovement();
                    transform.rotation = Quaternion.Euler(m_topViewPitch, m_yaw, 0.0f);
                    break;
                case CameraStates.PLANT_FOCUS:
                    FocusOnPlant();
                    break;
            }
        }

        private void DefaultMovement()
        {
            float3 min = m_leafRenderer.PointCloudTree.RootMin;
            float3 max = m_leafRenderer.PointCloudTree.RootMax;

            Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));

            movement = Quaternion.Euler(0.0f, m_yaw, 0.0f) * movement;

            Vector3 nextPosition = transform.position;
            nextPosition += movement * Time.deltaTime * m_moveSpeed;
            
            nextPosition.x = Math.Clamp(nextPosition.x, min.x, max.x);
            nextPosition.y = Math.Clamp(nextPosition.y, min.y, max.y);
            nextPosition.z = Math.Clamp(nextPosition.z, min.z, max.z);
            transform.position = nextPosition;
            m_leafRenderer.EnableCullingPlane = false;
            if (Input.GetKey(KeyCode.Q))
            {
                m_yaw -= Time.deltaTime * m_topViewTurnSpeed;
            }if (Input.GetKey(KeyCode.E))
            {
                m_yaw += Time.deltaTime * m_topViewTurnSpeed;
            }
        }

        private void FocusOnPlant()
        {
            if(m_selecedPlant == null) return;
            SetCullingPlane();
            
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetMouseButton(2))
            {
                m_yaw += (Input.mousePosition.x - m_lastMousePosition.x) * Time.deltaTime * m_sensitivity.x;
                m_pitch -= (Input.mousePosition.y - m_lastMousePosition.y) * Time.deltaTime * m_sensitivity.x;
                m_pitch = Mathf.Clamp(m_pitch, -89.0f, 89.0f);
            }
            transform.localRotation = Quaternion.Euler(m_pitch, m_yaw, 0.0f);
            transform.position = CalculateOrbitalPosition();
            m_lastMousePosition = Input.mousePosition;
        }
        private void SetCullingPlane()
        {
            Vector3 planeDirection = transform.forward;
            planeDirection.y = m_isCullInCamPlane ? planeDirection.y : 0.0f;
            m_leafRenderer.PlaneDirection = planeDirection;
            Vector3 position = m_selecedPlant.transform.position;
            position.y = m_selecedPlant.BoundingBox.center.y;
            m_leafRenderer.PlanePosition = position;
        }
        private Vector3 CalculateOrbitalPosition()
        {
            Vector3 direction = Quaternion.Euler(m_pitch, 0.0f, 0.0f) * Vector3.forward;
            direction = Quaternion.Euler(0.0f, m_yaw, 0.0f) * direction;
            Vector3 target = m_selecedPlant.transform.position;
            target.y = m_selecedPlant.BoundingBox.center.y;
            m_focusDistance = Mathf.Clamp(m_focusDistance + (Input.mouseScrollDelta.y * Time.deltaTime * m_scrollSpeed), m_minFocus, m_maxFocus);
            return direction.normalized * m_focusDistance + target;
        }
        private void OnDestroy() {
            m_uiMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
            m_uiMediator.OnSelectionChanged.RemoveListener(HandleSelectionChanged);
        }

    }

}
