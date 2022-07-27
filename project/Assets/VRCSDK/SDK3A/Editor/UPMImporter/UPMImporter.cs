using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace VRC.SDK3A
{
    [InitializeOnLoad]
    public static class UPMImporter
    {
        static UPMImporter()
        {
            Install("com.unity.mathematics@1.2.5");
            Install("com.unity.burst@1.4.11");
        }
        
        public static bool Install(string id)
        {
            var request = Client.Add(id);
            while (!request.IsCompleted) {};
            if(request.Error != null)Debug.LogError(request.Error.message);
            return request.Error == null;
        }
    }

}