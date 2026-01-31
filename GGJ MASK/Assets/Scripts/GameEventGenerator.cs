using UnityEngine;
using System;
using System.Collections.Generic;

public enum EventType
{
    Popcorn,
    // Add more event types here as needed
}

[System.Serializable]
public class GameEvent
{
    public EventType eventType;
    public float triggerTime; // Time in seconds when to trigger this event
    [HideInInspector] public bool hasTriggered = false;
}

public class GameEventGenerator : MonoBehaviour
{
    public SimpleDrawCanvas simpleDrawCanvas;

    [Header("Event List")]
    public List<GameEvent> events = new List<GameEvent>();

    private float elapsedTime = 0f;

    void Start()
    {
        // Sort events by trigger time to process them in order
        events.Sort((a, b) => a.triggerTime.CompareTo(b.triggerTime));
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // Check each event to see if it's time to trigger
        foreach (GameEvent gameEvent in events)
        {
            if (!gameEvent.hasTriggered && elapsedTime >= gameEvent.triggerTime)
            {
                TriggerEvent(gameEvent);
                gameEvent.hasTriggered = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Popcorn();
        }
    }

    /// <summary>
    /// Triggers an event based on its type.
    /// </summary>
    void TriggerEvent(GameEvent gameEvent)
    {
        Debug.Log($"Triggering event: {gameEvent.eventType} at {gameEvent.triggerTime}s");

        // Call appropriate method based on event type
        switch (gameEvent.eventType)
        {
            case EventType.Popcorn:
                Popcorn();
                break;

            default:
                Debug.LogWarning($"Unknown event type: {gameEvent.eventType}");
                break;
        }
    }

    void Popcorn()
    {
        //popcorn guy enter
        //popcorn machine blue to red
        //Boom sound effect
        if (simpleDrawCanvas != null)
            simpleDrawCanvas.MassiveHandShake();
    }
}
