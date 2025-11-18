namespace AnyVR.Voicechat
{
    public abstract class Participant
    {
        public readonly string Identity;

        internal readonly string Sid;

        internal Participant(string sid, string identity, string name)
        {
            Sid = sid;
            Identity = identity;
            Name = name;
        }

        public string Name { get; protected set; }
    }
}
