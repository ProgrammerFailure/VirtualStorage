using Expansions.Missions.Flow;
using KSP.UI.Screens.DebugToolbar.Screens.Cheats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
namespace VirtualStorage
{
    public class VirtualStorage : PartModule
    {
        #region Variables
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
        [KSPField(isPersistant = true, guiActive = true, guiName = "Current Resourcs", groupDisplayName = "Virtual Storage", groupName = "VirtualStorage"), UI_Cycle(scene = UI_Scene.Flight, stateNames = new string[] { "test", "blue" })]
        int CurrentResourceCycler = 0;

        [KSPField(isPersistant = true, guiName = "Request Amount", groupName = "VirtualStorage", groupDisplayName = "Virtual Storage", guiFormat = "F3", guiActive = true), UI_FloatRange(minValue = 0, maxValue = 1000, stepIncrement = 1)]
        float RequestAmount = 0;

        [KSPField(isPersistant = true, guiName = "Storage filled", groupName = "VirtualStorage", groupDisplayName = "Virtual Storage", guiActive = true)]
        string StorageDisplayString = "N/A";
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
        string[] ResourceBlacklist;

        //Part properties
        [KSPField(isPersistant = false, guiActive = true)]
        public float MaxStorage = 500;
        [KSPField(isPersistant = false, guiActive = true)]
        public string ResourceBlacklistString = "N/A";

        #endregion
        #region Triggers
        public void Start()
        {
            UpdateVesselResources();
            ((UI_Cycle)Fields["CurrentResourceCycler"].uiControlFlight).onFieldChanged = UpdateGUIResourceAmount;
            ResourceBlacklist = ResourceBlacklistString.Split(',');
        }
        override public void OnLoad(ConfigNode DataStorage) //Deserializing list
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            string KeysValue = null;
            string ValuesValue = null;
            DataStorage.TryGetValue("Keys", ref KeysValue);
            DataStorage.TryGetValue("Values", ref ValuesValue);
            Debug.Log($"OnLoad ConfigNode: {DataStorage}");
            string[] keys = KeysValue != null ? KeysValue.Split(',') : null;
            string[] values = ValuesValue != null ? ValuesValue.Split(',') : null;

            for (int i = 0; i < keys.Length; i++)
            {
                float.TryParse(keys[i], out float value);
                Debug.Log($"Virtual Storage mod, values list: {values}");
                Debug.Log($"Virtual Storage mod, values list: {keys}");
                Resources.Add(keys[i], value);
            }

            UpdateVesselResources();
            UpdateGUIResourceAmount();
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
            Debug.Log($"Virtual Storage mod, ResourceBlacklistString: {ResourceBlacklistString}\n ResourceBlacklist: {ResourceBlacklist}");
            if (ResourceBlacklist.Contains(CurrentResource)) { PopupBox($"{CurrentResource} is blacklisted for this Virtual Storage. Full blacklist: {ResourceBlacklistString}"); Debug.Log($"Virtual Storage mod: {CurrentResource} is blacklisted"); return; }
            if (RequestAmount <= 0) { return; }
            if (Resources.ContainsKey(CurrentResource) && VesselCurrentResourceAmount >= RequestAmount && (Resources.Values.Sum() + RequestAmount) <= MaxStorage)
            {
                this.vessel.RequestResource(this.part, CurrentResourceHash, RequestAmount, true);
                Resources[CurrentResource] += RequestAmount;
                UpdateGUIResourceAmount();
            }
            else if (VesselCurrentResourceAmount >= RequestAmount && (Resources.Values.Sum() + RequestAmount) <= MaxStorage)
            {
                this.vessel.RequestResource(this.part, CurrentResourceHash, RequestAmount, true);
                Resources.Add(CurrentResource, RequestAmount);
                UpdateGUIResourceAmount();
            }
            else if ((VesselCurrentResourceAmount >= RequestAmount) && (Resources.Values.Sum() + RequestAmount) > MaxStorage)
            {
                PopupBox($"Not enough free virtual storage");
            }
            else
            { 
                PopupBox($"Not enough {CurrentResource} in the vessel");
                Debug.Log($"Time.realtimeSinceStartup- Virtual Storage mod: Not enough {CurrentResource} in the vessel");
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
                else if (Resources[CurrentResource] == RequestAmount)
                {
                    this.vessel.RequestResource(this.part, CurrentResourceHash, -RequestAmount, true);
                    Resources.Remove(CurrentResource);
                }
                else //Resources[CurrentResource] < RequestAmount
                {
                    PopupBox($"Not enough {CurrentResource} in Virtual Storage");
                    Debug.Log($"{Time.realtimeSinceStartup}- Virtual Storage mod: Not enough {CurrentResource} in Virtual Storage");
                }

            }
            else
            {
                PopupBox($"Virtual Storage does not contain {CurrentResource}");
                Debug.Log($"{Time.realtimeSinceStartup}- Virtual Storage mod: Virtual Storage does not contain {CurrentResource}");
            }
            Debug.Log(Resources.Keys);
            Debug.Log(Resources.Values);
            UpdateGUIResourceAmount();
        }

        [KSPEvent(guiActive = true, guiName = "Virtual Storage- UpdateVesselResources", isPersistent = true, groupDisplayName = "Virtual Storage", groupName = "VirtualStorage")]
        private void UpdateVesselResources()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
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

        private void UpdateGUIResourceAmount(BaseField field = null, object oldValue = null)
        {
            List<string> keysToRemove = new List<string>();
            foreach (string key in Resources.Keys)
            {
                if (Resources[key] <= 0) { keysToRemove.Add(key); }
            }

            foreach (string keyToRemove in keysToRemove)
            {
                Resources.Remove(keyToRemove);
            }

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
                guiStorageCurrentResourceAmount = "";
            }
            StorageDisplayString = $"{Resources.Values.Sum()}/{MaxStorage}";
        }

        private void PopupBox(string message)
        {
            PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(
                    "VirtualStorageDialog", // Unique dialog ID
                    null, // No additional content, as we use DialogGUILabel below
                    "Virtual Storage Mod", // Title of the dialog
                    HighLogic.UISkin, // Use the KSP UI skin
                    new Rect(0.5f, 0.5f, 300f, 200f), // Centered, with initial size
                    new DialogGUIContentSizer(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.MinSize), // Adjust size to fit content
                    new DialogGUILabel(message), // Display the variable message
                    new DialogGUIButton("OK", () => { }, true) // OK button that dismisses the dialog
                ),
                false, // Don't pause the game when the dialog appears
                HighLogic.UISkin // Use the KSP UI skin for the popup
            );
        }
        #endregion
    }
}
