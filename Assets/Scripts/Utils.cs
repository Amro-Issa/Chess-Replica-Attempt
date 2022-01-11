using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    public static Vector3 GetMouseWorldPosition()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}
