using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PCMTool.ShowcaseSample
{
    [RequireComponent(typeof(Canvas))]
    public class ColorSelectionUI : MonoBehaviour
    {
        public UnityEvent OnColorChangeEvent {get;} = new UnityEvent();
        public Color SelectedColor {get; private set;} = Color.black;
        [SerializeField] private Image m_colorPreview;
        [SerializeField] private Slider m_hueSlider;
        [SerializeField] private Slider m_saturationSlider;
        [SerializeField] private Slider m_valueSlider;
        private void Awake() {
            m_hueSlider.onValueChanged.AddListener(HandleColorChange);
            m_saturationSlider.onValueChanged.AddListener(HandleColorChange);
            m_valueSlider.onValueChanged.AddListener(HandleColorChange);
            m_colorPreview.color = SelectedColor;
            Hide();
        }
        private void OnDestroy() {
            m_hueSlider.onValueChanged.RemoveAllListeners();
            m_saturationSlider.onValueChanged.RemoveAllListeners();
            m_valueSlider.onValueChanged.RemoveAllListeners();
        }
        private void HandleColorChange(float value)
        {
            float h = m_hueSlider.normalizedValue;
            float s = m_saturationSlider.normalizedValue;
            float v = m_valueSlider.normalizedValue;
            SelectedColor = Color.HSVToRGB(h,s,v);
            m_colorPreview.color = SelectedColor;
            OnColorChangeEvent.Invoke();
        }
        public void Show()
        {
            GetComponent<Canvas>().enabled = true;
        }
        public void Hide()
        {
            GetComponent<Canvas>().enabled = false;
        }
    }

}