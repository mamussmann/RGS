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
using RGS.Interaction;
using RGS.Models;
using UnityEngine;

namespace RGS.UI
{
    public class UISeedSignPool : MonoBehaviour
    {
        [SerializeField] private GameObject m_signPrefab;
        private List<UISeedSign> m_seedSigns = new List<UISeedSign>();
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private void Awake() {
            m_interactionMediator.OnNewSeedAdded.AddListener(HandleNewSeedAdded);
        }

        private void HandleNewSeedAdded(PlantSeedModel model)
        {
            var instance = Instantiate(m_signPrefab, transform);
            UISeedSign seedSign = instance.GetComponent<UISeedSign>();
            seedSign.Setup(model);
            m_seedSigns.Add(seedSign);
        }

        private void Update()
        {
            foreach (var sign in m_seedSigns)
            {
                sign.UpdateScreenPosition();
            }
        }

        private void OnDestroy() {
            m_interactionMediator.OnNewSeedAdded.RemoveListener(HandleNewSeedAdded);
        }
    }

}
