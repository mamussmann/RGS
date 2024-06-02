using System;
using UnityEngine;
using UnityEngine.UI;

namespace PCMTool.ShowcaseSample
{
    [RequireComponent(typeof(Canvas))]
    public class ToolbarUI : MonoBehaviour
    {
        [SerializeField] private PCMSculptor m_pcmSculptor;
        [SerializeField] private ColorSelectionUI m_colorSelectionUI;
        [SerializeField] private Button m_colorSelectionToggleButton;
        [SerializeField] private Image m_selectedColor;
        [SerializeField] private Slider m_brushSizeSlider;
        [SerializeField] private Button m_editButton;
        [SerializeField] private Button m_drawButton;
        [SerializeField] private Button m_scaleButton;
        [SerializeField] private Button m_cubePrimitiveButton;
        [SerializeField] private InputField m_plyFileInput;
        [SerializeField] private Button m_saveButton;
        [SerializeField] private Button m_loadButton;
        [SerializeField] private Dropdown m_detailDropdown;
        private bool m_inColorSelection;
        private void Awake()
        {
            m_colorSelectionToggleButton.onClick.AddListener(HandleColorSelection);
            m_colorSelectionUI.OnColorChangeEvent.AddListener(HandleColorChange);
            m_pcmSculptor.OnBrushSizeChange.AddListener(HandleBrushSizeChange);
            m_brushSizeSlider.onValueChanged.AddListener(HandleBrushSizeSliderChange);
            m_selectedColor.color = m_colorSelectionUI.SelectedColor;
            m_pcmSculptor.DrawColor = m_colorSelectionUI.SelectedColor;
            m_brushSizeSlider.normalizedValue = m_pcmSculptor.GetNormalizedBrushScale();
            UpdateButtonStates();
            m_editButton.onClick.AddListener(HandleEditButtonClicked);
            m_drawButton.onClick.AddListener(HandleEditDrawButtonClicked);
            m_scaleButton.onClick.AddListener(HandleScaleDrawButtonClicked);
            m_saveButton.onClick.AddListener(HandleSaveButtonClicked);
            m_loadButton.onClick.AddListener(HandleLoadButtonClicked);
            m_cubePrimitiveButton.onClick.AddListener(HandleCreateCubePrimitive);
        }

        private string GetFilePath()
        {
            return $"{Application.dataPath}/{m_plyFileInput.text}";
        }

        private void HandleSaveButtonClicked()
        {
            m_pcmSculptor.SaveAsPly(GetFilePath());
        }

        private void HandleLoadButtonClicked()
        {
            m_pcmSculptor.LoadFromPly(GetFilePath(), m_detailDropdown.value);
        }
        private void HandleCreateCubePrimitive()
        {
            m_pcmSculptor.CreateCubePrimitve();
        }

        private void OnDestroy() {
            m_colorSelectionToggleButton.onClick.RemoveAllListeners();
            m_colorSelectionUI.OnColorChangeEvent.RemoveAllListeners();
            m_pcmSculptor.OnBrushSizeChange.RemoveAllListeners();
            m_brushSizeSlider.onValueChanged.RemoveAllListeners();
            m_editButton.onClick.RemoveAllListeners();
            m_drawButton.onClick.RemoveAllListeners();
            m_scaleButton.onClick.RemoveAllListeners();
            m_cubePrimitiveButton.onClick.RemoveAllListeners();
        }
        private void HandleEditButtonClicked()
        {
            m_pcmSculptor.CurrentEditMode = EditMode.EDIT;
            UpdateButtonStates();
        }
        private void HandleEditDrawButtonClicked()
        {
            m_pcmSculptor.CurrentEditMode = EditMode.DRAW;
            UpdateButtonStates();
        }
        private void HandleScaleDrawButtonClicked()
        {
            m_pcmSculptor.CurrentEditMode = EditMode.SCALE;
            UpdateButtonStates();
        }
        private void UpdateButtonStates()
        {
            m_editButton.interactable = m_pcmSculptor.CurrentEditMode != EditMode.EDIT;
            m_drawButton.interactable = m_pcmSculptor.CurrentEditMode != EditMode.DRAW;
            m_scaleButton.interactable = m_pcmSculptor.CurrentEditMode != EditMode.SCALE;
        }
        private void HandleBrushSizeSliderChange(float arg0)
        {
            m_pcmSculptor.SetBrushScale(m_brushSizeSlider.normalizedValue);
        }

        private void HandleBrushSizeChange()
        {
            m_brushSizeSlider.normalizedValue = m_pcmSculptor.GetNormalizedBrushScale();
        }

        private void HandleColorChange()
        {
            m_selectedColor.color = m_colorSelectionUI.SelectedColor;
            m_pcmSculptor.DrawColor = m_colorSelectionUI.SelectedColor;
        }
        private void HandleColorSelection()
        {
            m_inColorSelection = !m_inColorSelection;
            if(m_inColorSelection){
                m_colorSelectionUI.Show();
            }else{
                m_colorSelectionUI.Hide();
            }
        }
    }

}