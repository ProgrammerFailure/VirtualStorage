using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualStorage
{
    public class VirtualStorage : PartModule
    {
        [KSPField(isPersistant = true)]
        List<string> ResourcesStored = new List<string>();

        [KSPField(isPersistant = true)]
        List<string> ResourceAmounts = new List<string>();

        [KSPField(isPersistant = true)]
        List<string> VesselResources = new List<string>();

        [KSPField(isPersistant = true)]
        string CurrentResource;

        [KSPField(isPersistant = true, guiActive = true, groupName = "Resources")]
        string ResourcesDisplayString;

        [KSPField(isPersistant = true)]
        int RequestAmount;
        public void Start()
        {
            foreach (Part part in this.vessel.Parts)
            {
                foreach (PartResource resource in part.Resources)
                {
                    if (!VesselResources.Contains(resource.resourceName))
                    {
                        {
                            VesselResources.Add(resource.resourceName);
                        }
                    }
                }
            }
        }
        public void UpdateStoredResources()
        {
            foreach (Part part in this.vessel.Parts)
            {
                foreach (PartResource resource in part.Resources)
                {
                    if (!VesselResources.Contains(resource.resourceName))
                    {
                        {
                            VesselResources.Add(resource.resourceName);
                        }
                    }
                }
            }
            for (int i = 0; i < ResourcesStored.Count; i++)
            {
                ResourcesDisplayString += ResourcesStored[i] + ": " + ResourceAmounts[i] + "\n";
            }
        }
        private void AddResource()
        {
            this.part.vessel.RequestResource(this.part, PartResourceLibrary.Instance.GetDefinition(CurrentResource).id,)
        }

    }
}
