using Project.Quadtree.AStart;
using Project.Quadtree.Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Console = Project.Development.Console;

namespace Project.Units
{
    [AlwaysUpdateSystem]
    public partial class UnitsMovementSystem : SystemBase
    {
        private const float Speed = 4;
        private Console _console;
        
        public NativeArray<byte> team1FlowField;
        public NativeArray<byte> team2FlowField;
        
        public JobHandle MoveJobHandle {
            get;
            private set;
        }
        
        private float _divisionLength;
        private int _byteFieldSideLenght;
        private EntityQuery _regularMoveQuery;
        private FlowFieldSystem _flowFieldSystem;
        
        protected override void OnStartRunning()
        {
            _console = Console.instance;
            _byteFieldSideLenght = _console.FlowFieldDivisions + 1;
            _divisionLength = _console.TotalBounds.z/ _byteFieldSideLenght;
            _regularMoveQuery = EntityManager.CreateEntityQuery(
                ComponentType.Exclude<AddTag>(),
                ComponentType.Exclude<RemoveTag>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadOnly<TeamData>());
            _flowFieldSystem = World.GetOrCreateSystem<FlowFieldSystem>();
        }
        protected override void OnUpdate()
        {
            if (team1FlowField.Length == 0)
                return;
            
            _flowFieldSystem.UpdateFieldJobHandle.Complete();
            MoveJobHandle = new MoveJob
            {
                team1FlowField = team1FlowField,
                team2FlowField = team2FlowField,
                divisionLength = _divisionLength,
                byteFieldSideLenght = _byteFieldSideLenght,
                deltaTime = Time.DeltaTime
            }.ScheduleParallel(_regularMoveQuery);
        }

        protected override void OnDestroy()
        {
            team1FlowField.Dispose();
            team2FlowField.Dispose();
        }

        [BurstCompile]
        private partial struct MoveJob : IJobEntity
        {
            [ReadOnly]
            public NativeArray<byte> team1FlowField;
            [ReadOnly]
            public NativeArray<byte> team2FlowField;
            
            [ReadOnly]
            public float divisionLength;
            [ReadOnly]
            public int byteFieldSideLenght;
            [ReadOnly]
            public float deltaTime;
            public void Execute(ref Translation translation, in TeamData teamData)
            {
                var coord = FlowFieldUtils.GetPosInFlowField(divisionLength, translation.Value.xz);
                if (teamData.team == Teams.Team0)
                {
                    var moveDirByte = team1FlowField[Native2dArrayUtils.CoordinatesToFlatIndex(coord, byteFieldSideLenght)];
                    var moveValue = FlowFieldUtils.ByteToDir(moveDirByte) * Speed;
                    var moveValue3 = new float3(moveValue.x, 0, moveValue.y);
                    translation.Value += moveValue3 * deltaTime;
                }
                else
                {
                    var moveDirByte = team2FlowField[Native2dArrayUtils.CoordinatesToFlatIndex(coord, byteFieldSideLenght)];
                    var moveValue = FlowFieldUtils.ByteToDir(moveDirByte) * Speed;
                    var moveValue3 = new float3(moveValue.x, 0, moveValue.y);
                    translation.Value += moveValue3 * deltaTime;
                }
            }
        }
    }
}