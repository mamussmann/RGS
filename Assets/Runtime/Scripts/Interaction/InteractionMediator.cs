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
using RGS.Configuration.UI;
using RGS.Models;
using UnityEngine.Events;

namespace RGS.Interaction
{

    public class InteractionMediator 
    {
        public UnityEvent<PieMenuActionType, int> OnPieActionSelected {get;} = new UnityEvent<PieMenuActionType, int>();
        public UnityEvent<PlantSeedModel> OnNewSeedAdded {get;} = new UnityEvent<PlantSeedModel>();
        public UnityEvent OnMagneticFieldUpdate {get;} = new UnityEvent();
        public UnityEvent<long, string, bool> OnApplyPartialDerivation {get;} = new UnityEvent<long, string, bool>();
        public UnityEvent<long, float, PlantSeedModel> OnCutRootAt {get;} = new UnityEvent<long, float, PlantSeedModel>();
        public UnityEvent<long, float, PlantSeedModel> OnPreviewCutRootAt {get;} = new UnityEvent<long, float, PlantSeedModel>();
        private static InteractionMediator m_instance;
        public static InteractionMediator Get() {
            if(m_instance == null) {
                m_instance = new InteractionMediator();
            }
            return m_instance;
        }
    }

}
