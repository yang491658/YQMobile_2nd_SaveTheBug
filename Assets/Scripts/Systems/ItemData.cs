using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Item", menuName = "ItemData", order = 2)]
public class ItemData : EntityData
{
    [Header("Item")]
    public MonoScript Script;
    public int Sort;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        var sprites = Resources.LoadAll<Sprite>("Images/Items");
        var used = new System.Collections.Generic.HashSet<string>();
        foreach (var g in AssetDatabase.FindAssets("t:ItemData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null && d != this && d.Image != null)
                used.Add(d.Image.name);
        }

        Sprite pick = null;
        if (Image == null || used.Contains(Image.name))
        {
            foreach (var s in sprites)
            {
                if (!used.Contains(s.name)) { pick = s; break; }
            }
            Image = pick;
        }

        if (Image != null)
        {
            var m = Regex.Match(Image.name, @"^(?<num>\d+)\.");
            ID = m.Success ? int.Parse(m.Groups["num"].Value) : ID;
        }
        else ID = 0;

        if (!string.IsNullOrEmpty(Name))
        {
            string[] guids = AssetDatabase.FindAssets(Name + " t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var cls = ms != null ? ms.GetClass() : null;
                if (cls != null && typeof(Item).IsAssignableFrom(cls) && cls.Name == Name)
                {
                    Script = ms;
                    break;
                }
            }
        }

        base.OnValidate();
        EditorUtility.SetDirty(this);
    }
#endif

    new public ItemData Clone()
    {
        var clone = CreateInstance<ItemData>();
        
        clone.name = this.Name;

        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.Image = this.Image;
        clone.Script = this.Script;
        clone.Sort = this.Sort;
        
        return clone;
    }
}
