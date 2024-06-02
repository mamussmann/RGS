using UnityEngine;
using UnityEngine.Events;

namespace PCMTool.ShowcaseSample
{
    public interface IPCMSculptor 
    {
        UnityEvent OnBrushSizeChange {get;}
        Color DrawColor { get; set; }
        EditMode CurrentEditMode {get; set;} 
        float GetNormalizedBrushScale();
        void SetBrushScale(float normalizedValue);
    }
}