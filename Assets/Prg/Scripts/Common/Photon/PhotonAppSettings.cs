﻿using Photon.Realtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace Prg.Scripts.Common.Photon
{
    /// <summary>
    /// Wrapper to Photon <c>AppSettings</c> so we can always have well defined Photon <c>AppSettings</c> that are never overwritten by Photon.
    /// </summary>
    /// <remarks>
    /// There has been bugs and problems (earlier) when using Photon <c>AppSettings</c> directly from Photon <c>Resources</c> folder.<br />
    /// Furthermore when you create new project from version control Photon overwrites our settings. We do not want this!
    /// </remarks>
    //[CreateAssetMenu(menuName = "ALT-Zone/PhotonAppSettings")]
    public class PhotonAppSettings : ScriptableObject
    {
        private const string Tooltip = "Use menu 'Window->Photon Unity Networking->Highlight Server Settings' to see other Photon settings";

        [Tooltip(Tooltip)] public AppSettings _appSettings;

        public override string ToString()
        {
            return _appSettings != null ? _appSettings.ToStringFull() : string.Empty;
        }
    }
}