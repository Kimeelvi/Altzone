using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DebugUi.Scripts.BattleAnalyzer
{
    public class LogPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textField;
        public GameObject Panel;

        public void OpenPanel()
        {
            if (Panel != null)
            {
                bool isActive = Panel.activeSelf;
                Panel.SetActive(!isActive);
            }
        }
    }
}
