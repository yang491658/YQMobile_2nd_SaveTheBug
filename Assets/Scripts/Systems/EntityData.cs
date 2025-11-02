using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "Entity", menuName = "EntityData", order = 1)]
public class EntityData : ScriptableObject
{
    [Header("Entity")]
    public int ID;
    public string Name;
    public Sprite Image;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (Image != null)
        {
            string rawName = Image.name;
            Name = Regex.Replace(rawName, @"^\d+\.", "");
        }
        else Name = null;
    }
#endif

    public EntityData Clone()
    {
        EntityData clone = CreateInstance<EntityData>();

        clone.name = this.Name;

        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.Image = this.Image;

        return clone;
    }
}
