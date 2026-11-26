using UnityEngine;

public interface IDeflectable
{
    void Deflect(Vector3 newDirection, GameObject newOwner);
}