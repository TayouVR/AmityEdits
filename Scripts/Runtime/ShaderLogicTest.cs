using UnityEditor;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    public class ShaderLogicTest: MonoBehaviour {
        
        
        
        // Penetrator Options
        [Header( "Penetrator Options" )]
        public bool _PenetratorEnabled = true;
        [Range(0, 1)]
        public float _DeformStrength = 1;
        public Vector3 _StartPosition = new Vector3(0, 0, 0);
        public Vector3 _StartRotation = new Vector3(0, 0, 0);
        public float _PenetratorLength = 0.2f;
        
        [Header( "Debug Orifice Position")]
        public Transform Orifice1Transform;
        public Transform Orifice2Transform;
        public int sampleCount = 100;
        public Mesh debugMesh;
        public bool useDebugMesh = true;
        
        [Header( "Bezier Curve Options" )]
        [Range(0, 1)]
        [Tooltip( "Size of the handles for the bezier curve, in percent to the penetrator length" )]
        public float bezierHandleSize = 0.25f;
//        [Enum(Channel 0,0,Channel 1,1)]_OrificeChannel("Orifice Channel",Float) = 0 
        //
        // [Header(Penetrator Legacy)]
        // [Toggle(_USE_IDS)] _UseIDs("Use IDs", Float) = 0
        // _ID_Orifice("ID Oriface", Float) = 0
        // _ID_RingOrifice("ID Ring Oriface", Float) = 0
        // _ID_Normal("ID Normal", Float) = 0
    }
}