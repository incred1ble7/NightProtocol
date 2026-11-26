using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NPInteriorAudio : MonoBehaviour
{
    [Serializable]
    private sealed class Interior
    {
        [SerializeField] private BoxCollider[] zones;
        [Tooltip("Optional override. An empty field uses Default Indoor Ambient.")]
        [SerializeField] private EventReference indoorAmbient;

        public EventReference Ambient => indoorAmbient;

        public bool Contains(Vector3 position)
        {
            if (zones == null)
                return false;

            foreach (var zone in zones)
            {
                if (!zone || !zone.enabled || !zone.gameObject.activeInHierarchy)
                    continue;

                Vector3 offset = zone.transform.InverseTransformPoint(position) - zone.center;
                Vector3 halfSize = zone.size * 0.5f;
                if (Mathf.Abs(offset.x) <= halfSize.x && Mathf.Abs(offset.y) <= halfSize.y
                    && Mathf.Abs(offset.z) <= halfSize.z)
                    return true;
            }

            return false;
        }
    }

    private sealed class IndoorTrack
    {
        public EventReference Reference;
        public EventInstance Instance;
        public float Volume;
        public bool Unavailable;
        public bool IsSpatial;
    }

    [SerializeField] private CharacterController player;
    [SerializeField] private StudioEventEmitter forestEmitter;
    [SerializeField] private EventReference defaultIndoorAmbient;
    [Tooltip("Optional FMOD snapshot with an authored attack and release on the exterior ambience bus.")]
    [SerializeField] private EventReference interiorLowPassSnapshot;
    [SerializeField, Range(0f, 1f)] private float forestVolumeIndoors = 0.28f;
    [SerializeField, Min(0.05f)] private float transitionSeconds = 0.75f;
    [SerializeField] private Interior tower = new Interior();
    [SerializeField] private Interior generatorRoom = new Interior();
    [SerializeField] private Interior playerHouse = new Interior();

    private readonly List<IndoorTrack> indoorTracks = new List<IndoorTrack>();
    private Interior currentInterior;
    private EventInstance lowPass;
    private bool lowPassRequested;
    private float indoorBlend;

    public bool IsInside => isActiveAndEnabled && FindInterior() != null;

    private void OnEnable()
    {
        currentInterior = FindInterior();
        indoorBlend = currentInterior == null ? 0f : 1f;
    }

    private void LateUpdate()
    {
        currentInterior = FindInterior();
        bool inside = currentInterior != null;
        float delta = Time.unscaledDeltaTime / Mathf.Max(transitionSeconds, 0.05f);
        indoorBlend = Mathf.MoveTowards(indoorBlend, inside ? 1f : 0f, delta);
        SetForestVolume(Mathf.Lerp(1f, forestVolumeIndoors, indoorBlend));

        EventReference target = inside ? currentInterior.Ambient : default;
        if (inside && target.IsNull)
            target = defaultIndoorAmbient;
        if (!target.IsNull)
            StartIndoorTrack(target);

        foreach (var track in indoorTracks)
        {
            if (!track.Instance.isValid())
                continue;

            bool selected = !target.IsNull && track.Reference.Guid.Equals(target.Guid);
            track.Volume = Mathf.MoveTowards(track.Volume, selected ? 1f : 0f, delta);
            track.Instance.setVolume(track.Volume);
            if (track.IsSpatial && player)
                track.Instance.set3DAttributes(RuntimeUtils.To3DAttributes(player.transform.TransformPoint(player.center)));

            if (!selected && track.Volume <= 0f)
                StopAndRelease(ref track.Instance);
        }

        UpdateLowPass(inside);
    }

    private void OnDisable()
    {
        SetForestVolume(1f);
        foreach (var track in indoorTracks)
            StopAndRelease(ref track.Instance);

        indoorTracks.Clear();
        StopAndRelease(ref lowPass);
        lowPassRequested = false;
        currentInterior = null;
        indoorBlend = 0f;
    }

    private Interior FindInterior()
    {
        if (!player || !player.enabled || !player.gameObject.activeInHierarchy)
            return null;

        Vector3 position = player.transform.TransformPoint(player.center);
        if (currentInterior != null && currentInterior.Contains(position))
            return currentInterior;
        if (tower.Contains(position))
            return tower;
        if (generatorRoom.Contains(position))
            return generatorRoom;
        return playerHouse.Contains(position) ? playerHouse : null;
    }

    private void SetForestVolume(float volume)
    {
        if (!forestEmitter)
            return;

        var instance = forestEmitter.EventInstance;
        if (instance.isValid())
            instance.setVolume(volume);
    }

    private void StartIndoorTrack(EventReference reference)
    {
        IndoorTrack track = null;
        foreach (var existing in indoorTracks)
        {
            if (existing.Reference.Guid.Equals(reference.Guid))
            {
                track = existing;
                break;
            }
        }
        if (track == null)
        {
            track = new IndoorTrack { Reference = reference };
            indoorTracks.Add(track);
        }

        if (track.Unavailable || track.Instance.isValid())
            return;

        track.Instance = CreateInstance(reference);
        track.Unavailable = !track.Instance.isValid();
        if (track.Unavailable)
            return;

        track.Instance.getDescription(out var description);
        description.is3D(out track.IsSpatial);
        if (track.IsSpatial)
            track.Instance.set3DAttributes(RuntimeUtils.To3DAttributes(player.transform.TransformPoint(player.center)));
        track.Volume = 0f;
        track.Instance.setVolume(0f);
        track.Instance.start();
    }

    private void UpdateLowPass(bool inside)
    {
        if (interiorLowPassSnapshot.IsNull || lowPassRequested == inside)
            return;

        lowPassRequested = inside;
        if (inside)
        {
            if (!lowPass.isValid())
                lowPass = CreateInstance(interiorLowPassSnapshot);
            if (lowPass.isValid())
                lowPass.start();
        }
        else if (lowPass.isValid())
        {
            lowPass.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }

    private EventInstance CreateInstance(EventReference reference)
    {
        try
        {
            return RuntimeManager.CreateInstance(reference);
        }
        catch (EventNotFoundException)
        {
            Debug.LogWarning($"FMOD event is unavailable in the loaded banks: {reference}", this);
            return default;
        }
    }

    private static void StopAndRelease(ref EventInstance instance)
    {
        if (instance.isValid())
        {
            instance.stop(STOP_MODE.IMMEDIATE);
            instance.release();
        }

        instance = default;
    }
}
