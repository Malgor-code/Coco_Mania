
public readonly struct AudioContextChanged : IEvent
{
    public readonly AudioContext Context;
    public AudioContextChanged(AudioContext context) { Context = context; }
}