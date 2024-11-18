using Expansions.Missions.Flow;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace VirtualStorage
{
    public class VirtualStorage : PartModule
    {
        #region Variables
        //Serialization lists
        string ResourceList;
        string ResourceAmounts;

        //Current values
        [KSPField(isPersistant = false, guiActive = true, guiName = "Current Resource Amounts", groupDisplayName = "Virtual Storage Resources", groupName = "VirtualStorageResources")]
        string guiStorageCurrentResourceAmount;
        string CurrentResource
        {
            get
            {
                Debug.Log("VirtualStorage: CurrentResource fetched, value = " + VesselResourceList[CurrentResourceCycler].resourceName);
                return VesselResourceList[CurrentResourceCycler].resourceName;
            }
        }
        [KSPField(isPersistant = true, guiActive = true, guiName = "Current Resourcs", groupDisplayName = "Virtual Storage", groupName = "VirtualStorage"), UI_Cycle(scene = UI_Scene.Flight, stateNames = new string[] {"test", "blue"})]
        int CurrentResourceCycler = 0;

        [KSPField(isPersistant = true, guiName = "Request Amount", groupName = "VirtualStorage", groupDisplayName = "Virtual Storage", guiFormat = "F3", guiActive = true), UI_FloatRange(minValue = 0, maxValue = 1000, stepIncrement = 1)]
        float RequestAmount = 0;
        float StorageCurrentResourceAmount
        {
            get
            {
                return Resources[CurrentResource];
            }
        }
        //Vessel current values
        double VesselCurrentResourceAmount
        {
            get
            {
                this.vessel.GetConnectedResourceTotals(CurrentResourceHash, out double fillerAmount, out double fillerMaxAmount);
                return fillerAmount;
            }
        }
        double VesselCurrentResourceMaxAmount
        {
            get
            {
                this.vessel.GetConnectedResourceTotals(CurrentResourceHash, out double fillerAmount, out double fillerMaxAmount);
                return fillerMaxAmount;
            }
        }
        int CurrentResourceHash
        {
            get
            {
                return PartResourceLibrary.Instance.GetDefinition(CurrentResource).id;
            }
        }
        //Actual list
        Dictionary<string, float> Resources = new Dictionary<string, float>();
        List<PartResource> VesselResourceList = new List<PartResource>();
        #endregion
        #region Triggers
        public void Start()
        {
            UpdateVesselResources();
            ((UI_Cycle)Fields["CurrentResourceCycler"].uiControlFlight).onFieldChanged = UpdateGUIResourceAmount;

        }
        override public void OnLoad(ConfigNode DataStorage) //Deserializing list
        {
            string KeysValue = null;
            string ValuesValue = null;
            DataStorage.TryGetValue("Keys", ref KeysValue);
            DataStorage.TryGetValue("Values", ref ValuesValue);
            string[] keys = KeysValue.Split(',');
            string[] values = ValuesValue.Split(',');
            for (int i = 0; i < keys.Length; i++)
            {
                Resources.Add(keys[i], float.Parse(values[i]));
            }
            UpdateVesselResources();
        }
        override public void OnSave(ConfigNode DataStorage) //Serializing list
        {
            DataStorage.AddValue("Keys", string.Join(",", Resources.Keys));
            DataStorage.AddValue("Values", string.Join(",", Resources.Values));
        }
        #endregion
        #region Methods
        [KSPEvent(guiActive = true, guiName = "Insert Resource", isPersistent = true, groupDisplayName = "Virtual Storage", groupName = "VirtualStorage")]
        public void AddResourceToStorage()
        {
            if (Resources.ContainsKey(CurrentResource) && VesselCurrentResourceAmount >= RequestAmount)
            {
                this.vessel.RequestResource(this.part, CurrentResourceHash, RequestAmount, true);
                Resources[CurrentResource] += RequestAmount;
                UpdateGUIResourceAmount();
            }
            else if (VesselCurrentResourceAmount >= RequestAmount)
            {
                this.vessel.RequestResource(this.part, CurrentResourceHash, RequestAmount, true);
                Resources.Add(CurrentResource, RequestAmount);
                UpdateGUIResourceAmount();
            }
            else
            {
                UpdateGUIResourceAmount();
                Debug.Log(Time.realtimeSinceStartup + "- Virtual Storage mod: Not enough " + CurrentResource + " in the vessel");
            }
            Debug.Log(Resources.Keys);
            Debug.Log(Resources.Values);

        }
        [KSPEvent(guiActive = true, guiName = "Extract Resource", isPersistent = true, groupDisplayName = "Virtual Storage", groupName = "VirtualStorage")]
        private void RemoveResourceFromStorage()
        {
            //if (Resources.ContainsKey(CurrentResource))
            //{
            //    switch (Resources[CurrentResource].CompareTo(RequestAmount))
            //    {
            //        case 1: // Resources[CurrentResource] > resourceAmount
            //            this.vessel.RequestResource(this.part, CurrentResourceHash, -RequestAmount, true);
            //            Resources[CurrentResource] -= Convert.ToSingle(RequestAmount);
            //            break;

            //        case 0: // Resources[CurrentResource] == resourceAmount
            //            this.vessel.RequestResource(this.part, CurrentResourceHash, -RequestAmount, true);
            //            Resources.Remove(CurrentResource);
            //            break;

            //        case -1: // Resources[CurrentResource] < resourceAmount
            //            Debug.Log(Time.realtimeSinceStartup + "- Virtual Storage mod: Not enough " + CurrentResource + " in Virtual Storage");
            //            break;
            //    }
            //}
            if (Resources.ContainsKey(CurrentResource))
            {
                if (Resources[CurrentResource] > RequestAmount)
                {
                    this.vessel.RequestResource(this.part, CurrentResourceHash, -RequestAmount, true);
                    Resources[CurrentResource] -= RequestAmount;
                }
                if (Resources[CurrentResource] == RequestAmount)
                {
                    this.vessel.RequestResource(this.part, CurrentResourceHash, -RequestAmount, true);
                    Resources.Remove(CurrentResource);
                }
                if (Resources[CurrentResource] < RequestAmount)
                {
                    Debug.Log(Time.realtimeSinceStartup + "- Virtual Storage mod: Not enough " + CurrentResource + " in Virtual Storage");
                }

            }
            else
            {
                Debug.Log(Time.realtimeSinceStartup + "- Virtual Storage mod: Virtual Storage does not contain " + CurrentResource);
            }
            Debug.Log(Resources.Keys);
            Debug.Log(Resources.Values);
            UpdateGUIResourceAmount();
        }

        [KSPEvent(guiActive = true, guiName = "Virtual Storage- UpdateVesselResources", isPersistent = true, groupDisplayName = "Virtual Storage", groupName = "VirtualStorage")]
        private void UpdateVesselResources()
        {
            VesselResourceList.Clear();
            foreach (Part part in this.vessel.parts)
            {
                foreach (PartResource resource in part.Resources)
                {
                    if (VesselResourceList.All(r => r.resourceName != resource.resourceName))
                    {
                        VesselResourceList.Add(resource);
                    }
                }
            }
            //Get just the UI_Cycle component from the field
            UI_Cycle cycler = Fields["CurrentResourceCycler"].uiControlFlight as UI_Cycle;

            /*Select(r => r.resourceName) extracts all the resourceName fields from the PartResource fields in VesselResourceList
             Then ToArray makes it into an array that replaces statenames*/
            cycler.stateNames = VesselResourceList.Select(r => r.resourceName).ToArray();

            //This section sets the stateNames to the resourceName fields from VesselResourceList.
            UpdateGUIResourceAmount();
        }

        private void UpdateGUIResourceAmount(BaseField field = null, object oldValue=null)
        {
            if (Resources.Count > 0)
            {
                string _field = "";
                Fields["guiStorageCurrentResourceAmount"].guiActive = true;
                foreach (string key in Resources.Keys)
                {
                    _field += $"\n{key}: {Resources[key]}";
                }
                guiStorageCurrentResourceAmount = _field;
            }
            else
            {
                Fields["guiStorageCurrentResourceAmount"].guiActive = false;
                guiStorageCurrentResourceAmount = null;
            }
        }

        #endregion
    }
}
