#ifndef APS_GLOBALS
#define APS_GLOBALS

#define LIGHT_ROLE_UNKNOWN 0
#define LIGHT_ROLE_HOLE 1
#define LIGHT_ROLE_RING_TWOWAY 2
#define LIGHT_ROLE_RING_ONEWAY 3
#define LIGHT_ROLE_NORMAL 4

struct LightInfo {
	float3 position;
	int role;
};

struct OrificeData {
	float3 position;
	float3 normal; // The calculated forward direction
	float3 normalLightPosition; // The raw position of the normal light
	int type;
	bool isValid;
};

float _OrificeChannel;
			
float _PenetratorEnabled;
float _PenetratorLength;
float3 _StartPosition;
float3 _StartRotation;
float _DeformStrength;
float _AllTheWayThrough;            

float _BezierHandleSize;
float _SplineDebug;

// TODO: see SPS, add blendshape baking feature, as some users may need it.
// TODO: maybe prefix all APS variables, when I have a better name than APS

#endif
