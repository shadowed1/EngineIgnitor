using System;
using UnityEngine;

namespace EngineIgnitor
{
    [Serializable]
    internal class KerbalInfo
    {
        public KerbalInfo()
        {
            VesselId = 0;
            KerbalName = string.Empty;
            Time = 0;
            FoundIgnitors = 0;
        }

        public int VesselId { get; set; }
        public string KerbalName { get; set; }
        public int Time { get; set; }
        public double FoundIgnitors { get; set; }
    }

    [Serializable]
    public class IgnitorResource : IConfigNode
    {
        [SerializeField]
        public string Name;
        [SerializeField]
        public float Amount;

        public float CurrentAmount;

        public void Load(ConfigNode node)
        {
            Name = node.GetValue("name");
            if (node.HasValue("amount"))
            {
                Amount = Mathf.Max(0.0f, float.Parse(node.GetValue("amount")));
            }
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("name", Name);
            node.AddValue("amount", Mathf.Max(0.0f, Amount));
        }

        public override string ToString()
        {
            return Name + "(" + Amount.ToString("F3") + ")";
        }

        public static IgnitorResource FromString(string str)
        {
            IgnitorResource ir = new IgnitorResource();
            int indexL = str.LastIndexOf('('); int indexR = str.LastIndexOf(')');
            ir.Name = str.Substring(0, indexL);
            ir.Amount = float.Parse(str.Substring(indexL + 1, indexR - indexL - 1));
            return ir;
        }
    }
}
