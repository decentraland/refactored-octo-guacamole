using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Feature
{
    public virtual void Initialize() { }
    public virtual void OnGUI() { }
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void Dispose() { }

}