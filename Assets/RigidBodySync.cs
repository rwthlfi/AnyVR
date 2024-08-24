using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using UnityEngine;

namespace AnyVR
{
    public class RigidbodySync : NetworkBehaviour
    {
        //Replicate structure.
        public struct ReplicateData : IReplicateData
        {
            //The uint isn't used but Unity C# version does not
            //allow parameter-less constructors we something
            //must be set as a parameter.
            public ReplicateData(uint unused) : this() {}
            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }
        //Reconcile structure.
        public struct ReconcileData : IReconcileData
        {
            public PredictionRigidbody PredictionRigidbody;
            
            public ReconcileData(PredictionRigidbody pr) : this()
            {
                PredictionRigidbody = pr;
            }
        
            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }

        //Forces are not applied in this example but you
        //could definitely still apply forces to the PredictionRigidbody
        //even with no controller, such as if you wanted to bump it with a player.
        private PredictionRigidbody _predictionRigidbody;

        [SerializeField] private float _force;
        
        private void Awake()
        {
            _predictionRigidbody = ObjectCaches<PredictionRigidbody>.Retrieve();
            _predictionRigidbody.Initialize(GetComponent<Rigidbody>());
        }
        private void OnDestroy()
        {
            ObjectCaches<PredictionRigidbody>.StoreAndDefault(ref _predictionRigidbody);
        }

        //In this example we do not need to use OnTick, only OnPostTick.
        //Because input is not processed on this object you only
        //need to pass in default for RunInputs, which can safely
        //be done in OnPostTick.
        public override void OnStartNetwork()
        {
            base.TimeManager.OnPostTick += TimeManager_OnPostTick;
        }

        public override void OnStopNetwork()
        {
            base.TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }

        private void TimeManager_OnPostTick()
        {
            RunInputs(default);
            CreateReconcile();
        }

        [Replicate]
        private void RunInputs(ReplicateData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            //If this object is free-moving and uncontrolled then there is no logic.
            //Just let physics do it's thing.	
            if (IsServerInitialized)
            {
                _predictionRigidbody.AddForce(new Vector3(0, _force * (float)TimeManager.TickDelta, 0));
            }
        }

        //Create the reconcile data here and call your reconcile method.
        public override void CreateReconcile()
        {
            ReconcileData rd = new ReconcileData(_predictionRigidbody);
            ReconcileState(rd);
        }

        [Reconcile]
        private void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            //Call reconcile on your PredictionRigidbody field passing in
            //values from data.
            _predictionRigidbody.Reconcile(data.PredictionRigidbody);
        }
    }

}
