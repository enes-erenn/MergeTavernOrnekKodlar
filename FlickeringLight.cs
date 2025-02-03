using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    [SerializeField] Light[] Lights;
    [SerializeField] FlickeringSettings Settings;

    public void Start()
    {
        Settings.Multiplier = Lights.Select((l) => l.intensity).Sum() / Lights.Length;
        StartCoroutine(Flicker());
    }

    /// <summary>
    /// Flickers the given light sources to pretend to be a gas light
    /// </summary>
    IEnumerator Flicker()
    {
        while (true)
        {
            float interval = Settings.Interval;

            if (UnityEngine.Random.value <= (1 - Settings.FlickeringChance))
                interval = UnityEngine.Random.Range(0, 3);

            yield return new WaitForSeconds(interval);

            if (interval != Settings.Interval)
                continue;

            for (int i = 0; i < Lights.Length; i++)
                Lights[i].intensity = UnityEngine.Random.Range(Settings.Min, Settings.Max) * Settings.Multiplier;
        }
    }
}

[Serializable]
public class FlickeringSettings
{
    [Range(0, 1)]
    public float Min, Max, FlickeringChance;

    /// <summary>
    /// If value is zero, then it always flickers
    /// </summary>
    public float Interval;

    /// <summary>
    /// Controls the intensity of the light sources using <c>Min</c> and <c>Max</c> values.
    /// </summary>
    [HideInInspector]
    public float Multiplier;
}
