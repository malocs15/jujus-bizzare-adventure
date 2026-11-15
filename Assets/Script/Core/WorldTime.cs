using UnityEngine;

public static class WorldTime
{
    public static float Scale = 1f;
    public static float DeltaTime => Time.deltaTime * Scale;
}