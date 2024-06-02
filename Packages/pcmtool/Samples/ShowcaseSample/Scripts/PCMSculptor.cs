using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using PCMTool.Tree;
using UnityEngine.Events;

namespace PCMTool.ShowcaseSample
{
    public enum EditMode
    {
        EDIT, DRAW, SCALE
    }
    public enum InteractionMode
    {
        SCALE, CUBE_DRAGGING, NORMAL
    }
    public class PCMSculptor : MonoBehaviour, IPCMSculptor
    {
        public UnityEvent OnBrushSizeChange { get; } = new UnityEvent();
        public Color DrawColor { get; set; }
        public EditMode CurrentEditMode { get; set; } = EditMode.EDIT;
        [SerializeField] private Camera m_camera;
        [SerializeField] private GameObject m_3dCursor;
        [SerializeField] private RuntimeLeafModifier m_sculpture;
        [SerializeField] private float m_minScale, m_maxScale;
        [SerializeField] private Vector2 m_sensitivity;
        [SerializeField] private Transform m_cubePreview;
        [SerializeField] private int m_addSpherePointCount = 5000;
        [SerializeField] private int m_addCubePointCount = 1000;
        [SerializeField] private int m_addPrimitiveCubePointCount = 10000;
        private InteractionMode m_currentInteractionMode = InteractionMode.NORMAL;
        private Vector3 m_pivot;
        private float m_pitch, m_yaw;
        private Vector3 m_direction = Vector3.forward;
        private float m_camDistance = 2.0f;
        private float m_scale = 1.0f;
        private float m_scaleDiff = 0.0f;
        private int m_lineIndex = 0;
        private Vector3 m_lastMousePosition;
        private Vector3 m_lastCarvePosition;
        private bool m_isCubeRemove;
        private Vector3[] m_cubeDirections = new Vector3[4];
        private readonly Vector3[] directions = new Vector3[3]{
                Vector3.right, Vector3.up, Vector3.forward
            };
        private void Awake() {    
            m_cubePreview.gameObject.SetActive(false);
        }
        private bool IsPointerOverUIElement()
        {
            var eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Where(x => x.gameObject.layer == LayerMask.NameToLayer("UI")).Count() > 0;
        }
        public float GetNormalizedBrushScale()
        {
            return (m_scale - m_minScale) / (m_maxScale - m_minScale);
        }
        public void SetBrushScale(float normalizedValue)
        {
            m_scale = (m_maxScale - m_minScale) * normalizedValue + m_minScale;
            m_3dCursor.transform.localScale = Vector3.one * m_scale;
        }
        private void UpdateScaleInteraction()
        {
            m_scaleDiff = (Input.mousePosition.x - m_lastMousePosition.x) / Screen.width;
            m_3dCursor.transform.localScale = Vector3.one * Mathf.Clamp((m_scale + m_scaleDiff), m_minScale, m_maxScale);
            if (Input.GetMouseButtonUp(0))
            {
                m_scale = Mathf.Clamp(m_scale + m_scaleDiff, m_minScale, m_maxScale);
                m_3dCursor.transform.localScale = Vector3.one * m_scale;
                OnBrushSizeChange.Invoke();
                m_currentInteractionMode = InteractionMode.NORMAL;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                m_3dCursor.transform.localScale = Vector3.one * m_scale;
                m_currentInteractionMode = InteractionMode.NORMAL;
            }
        }
        private void UpdateCubeInteraction()
        {
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            {
                if (m_lineIndex == 0)
                {
                    Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                    Vector3 hitPosition;
                    if (m_sculpture.HitPC(ray.origin, ray.direction, 0.1f, out hitPosition, false))
                    {
                        m_lineIndex = 1;
                        m_3dCursor.SetActive(false);
                        m_cubeDirections[0] = hitPosition;
                        m_lastMousePosition = Input.mousePosition;
                        m_isCubeRemove = Input.GetMouseButtonUp(1);
                        return;
                    }
                }
            }
            
            if (m_lineIndex >= 1)
            {
                m_cubePreview.gameObject.SetActive(true);
                var pos = m_cubeDirections[0];
                m_cubeDirections[m_lineIndex] = pos + (directions[m_lineIndex-1] * (Input.mousePosition - m_lastMousePosition).x * 0.01f);
                UpdateCubeSize();
                if (Input.GetMouseButtonUp(0))
                {
                    if(m_lineIndex >= 3) {
                        var origin = m_cubeDirections[0];
                        var dir1 = m_cubeDirections[1] - m_cubeDirections[0];
                        var dir2 = m_cubeDirections[2] - m_cubeDirections[0];
                        var dir3 = m_cubeDirections[3] - m_cubeDirections[0];

                        if (m_isCubeRemove)
                        {
                            m_sculpture.RemoveCube(origin, dir1, dir2, dir3);
                        }
                        else
                        {
                            m_sculpture.AddCube(origin, dir1, dir2, dir3, m_addCubePointCount, DrawColor);
                        }
                        m_3dCursor.SetActive(true);
                        m_cubePreview.gameObject.SetActive(false);
                        m_currentInteractionMode = InteractionMode.NORMAL;
                    }
                    m_lastMousePosition = Input.mousePosition;
                    m_lineIndex = m_lineIndex < 3 ?  m_lineIndex + 1 : 0;
                    return;
                }
            }
            
            if (Input.GetKeyUp(KeyCode.Q))
            {
                m_3dCursor.SetActive(true);
                m_currentInteractionMode = InteractionMode.NORMAL;
            }
        }
        private void UpdateCubeSize()
        {
            var origin = m_cubeDirections[0];
            var dir1 = m_cubeDirections[1] - m_cubeDirections[0];
            var dir2 = m_lineIndex >= 2 ? m_cubeDirections[2] - m_cubeDirections[0] : directions[1] * 0.1f;
            var dir3 = m_lineIndex >= 3 ? m_cubeDirections[3] - m_cubeDirections[0] : directions[2] * 0.1f;
            var scale = (dir1+dir2+dir3);
            var center = scale * 0.5f + origin;
            m_cubePreview.position = center;
            scale.x = Mathf.Abs(scale.x);
            scale.y = Mathf.Abs(scale.y);
            scale.z = Mathf.Abs(scale.z);
            m_cubePreview.localScale = scale;
        }
        private void UpdateNormalInteractionMode()
        {
            if (Input.GetKeyUp(KeyCode.F))
            {
                m_currentInteractionMode = InteractionMode.SCALE;
                m_scaleDiff = 0.0f;
            }
            if (Input.GetMouseButton(2))
            {
                m_pivot += (Input.mousePosition.x - m_lastMousePosition.x) * -0.02f * m_camera.gameObject.transform.right.normalized;
                m_pivot += (Input.mousePosition.y - m_lastMousePosition.y) * -0.02f * m_camera.gameObject.transform.up.normalized;
            }
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                m_yaw += (Input.mousePosition.x - m_lastMousePosition.x) * Time.deltaTime * m_sensitivity.x;
                m_pitch -= (Input.mousePosition.y - m_lastMousePosition.y) * Time.deltaTime * m_sensitivity.x;
                m_pitch = Mathf.Clamp(m_pitch, -89.0f, 89.0f);
            }
            m_direction = Quaternion.Euler(m_pitch, 0.0f, 0.0f) * Vector3.forward;
            m_direction = Quaternion.Euler(0.0f, m_yaw, 0.0f) * m_direction;
            m_camera.transform.localRotation = Quaternion.Euler(m_pitch, m_yaw, 0.0f);

            m_camDistance = Mathf.Min(m_camDistance + Input.mouseScrollDelta.y, 0.0f);
            m_camera.transform.position = m_direction.normalized * m_camDistance + m_pivot;
            Update3DCursor();

            if (Input.GetMouseButton(0))
            {
                Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                Vector3 hitPosition;
                if (m_sculpture.HitPC(ray.origin, ray.direction, m_scale * 0.5f, out hitPosition))
                {
                    switch (CurrentEditMode)
                    {
                        case EditMode.EDIT:
                            m_sculpture.Add(hitPosition, m_scale * 0.5f, m_addSpherePointCount);
                            break;
                        case EditMode.DRAW:
                            m_sculpture.SetColor(hitPosition, m_scale * 0.5f, DrawColor);
                            break;
                        case EditMode.SCALE:
                            m_sculpture.SetPointScale(hitPosition, m_scale * 0.5f, 0.05f);
                            break;
                    }
                }
            }
            else if (Input.GetMouseButton(1))
            {
                Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                Vector3 hitPosition;
                if (m_sculpture.HitPC(ray.origin, ray.direction, m_scale * 0.5f, out hitPosition))
                {
                    switch (CurrentEditMode)
                    {
                        case EditMode.EDIT:
                            m_sculpture.Carve(hitPosition, m_scale * 0.5f);
                            break;
                        case EditMode.DRAW:
                            m_sculpture.SetColor(hitPosition, m_scale * 0.5f, new Color(1.0f - DrawColor.r, 1.0f - DrawColor.g, 1.0f - DrawColor.b));
                            break;
                        case EditMode.SCALE:
                            m_sculpture.SetPointScale(hitPosition, m_scale * 0.5f, -0.05f);
                            break;
                    }
                }
            }

            if (Input.GetKeyUp(KeyCode.Q))
            {
                m_currentInteractionMode = InteractionMode.CUBE_DRAGGING;
            }

            m_lastMousePosition = Input.mousePosition;
        }
        private void Update()
        {
            if (IsPointerOverUIElement()) return;
            switch (m_currentInteractionMode)
            {
                case InteractionMode.NORMAL:
                    UpdateNormalInteractionMode();
                    break;
                case InteractionMode.CUBE_DRAGGING:
                    UpdateCubeInteraction();
                    break;
                case InteractionMode.SCALE:
                    UpdateScaleInteraction();
                    break;
            }
        }

        public void CreateCubePrimitve()
        {
            m_sculpture.AddCube(-Vector3.one, Vector3.forward * 2, Vector3.right * 2, Vector3.up * 2, m_addPrimitiveCubePointCount, DrawColor, false);
        }

        public void LoadFromPly(string file, int detail)
        {
            m_sculpture.LoadPlyFile(file, detail);
        }

        public void SaveAsPly(string file)
        {
            m_sculpture.SaveToPlyFile(file);
        }

        private void Update3DCursor()
        {
            var mousePos = Input.mousePosition;
            mousePos.z = 0.5f;
            Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
            Vector3 hitPosition;
            if (m_sculpture.HitPC(ray.origin, ray.direction, m_scale * 0.5f, out hitPosition))
            {
                m_3dCursor.SetActive(true);
                m_3dCursor.transform.position = hitPosition;
                m_3dCursor.transform.rotation = m_camera.transform.rotation;
            }
            else
            {
                m_3dCursor.transform.position = m_camera.ScreenToWorldPoint(mousePos);
                m_3dCursor.SetActive(false);
            }
        }
    }

}
