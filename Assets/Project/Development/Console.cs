using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Development
{
    public class Console : MonoBehaviour
    {
        public static Console instance;

        [SerializeField]
        private DrawingMode _drawingMode;
        
        private DrawingPerspective _drawingPerspective = DrawingPerspective.CUSTOM;
        
        [SerializeField]
        private int _FlowFieldDivisions;
        
        [SerializeField, Range(0f, 1f)]
        private float _timeScale;
        
        [SerializeField]
        private int _unitsPerFrameTeam1;

        [SerializeField]
        private int _unitsPerFrameTeam2;

        [SerializeField]
        private float4 _totalBounds;

        [SerializeField]
        private GameObject _unitToSpawnPrefab;

        [SerializeField]
        private Transform _team1SpawnPosition;
        [SerializeField]
        private Transform _team2SpawnPosition;

        private BlobAssetStore _blobAssetStore;

        private GameObjectConversionSettings _settings;
        

        public Entity unitToSpawnPrefab;
        
        public DrawingMode DrawingMode => _drawingMode;
        public DrawingPerspective DrawingPerspective => _drawingPerspective;
        public int FlowFieldDivisions => _FlowFieldDivisions;
        public int UnitsPerFrameTeam1 => _unitsPerFrameTeam1;
        public int UnitsPerFrameTeam2 => _unitsPerFrameTeam2;
        public float4 TotalBounds => _totalBounds;
        public float2 Team2TargetPosition => ((float3)_team1SpawnPosition.position).xz;
        public float2 Team1TargetPosition => ((float3)_team2SpawnPosition.position).xz;


        private void Awake()
        {
            instance = this;
            _blobAssetStore = new BlobAssetStore();
            _settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, _blobAssetStore);
        }

        private void Start()
        {
            DelayedStart();
        }

        private async void DelayedStart()
        {
            await Task.Delay(1000);
            unitToSpawnPrefab = ConvertGameObjectToEntity(_unitToSpawnPrefab);
        }

        private Entity ConvertGameObjectToEntity(GameObject gameObject)
        {
            return GameObjectConversionUtility.ConvertGameObjectHierarchy(gameObject, _settings);
        }
        
        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            if (!(_drawingMode == DrawingMode.NONE || _drawingMode == DrawingMode.SIMPLE_GRID))
            {
                if(_drawingPerspective == DrawingPerspective.CUSTOM)
                    _timeScale = 0f;
            }
            Time.timeScale = _timeScale;
        }
    }

    public enum DrawingMode
    {
        NONE,
        SIMPLE_GRID,
        NEIGHBOUR_GRID,
        WEIGHTS,
        SIMPLE_PATH,
        BEZIER_PATH,
        FLOW_FIELD,
    }
    
    public enum DrawingPerspective
    {
        CUSTOM,
        TEAM1,
        TEAM2
    }
}