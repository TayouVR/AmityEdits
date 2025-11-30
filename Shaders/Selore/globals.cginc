#ifndef SELORE_GLOBALS
#define SELORE_GLOBALS

#define SELORE_LIGHT_ROLE_UNKNOWN 0
#define SELORE_LIGHT_ROLE_HOLE 1
#define SELORE_LIGHT_ROLE_RING_TWOWAY 2
#define SELORE_LIGHT_ROLE_RING_ONEWAY 3
#define SELORE_LIGHT_ROLE_NORMAL 4

struct Selore_LightInfo {
	float3 position;
	int role;
};

struct Selore_OrificeData {
	float3 position;
	float3 normal; // The calculated forward direction
	float3 normalLightPosition; // The raw position of the normal light
	int type;
	bool isValid;
};

float Selore_Channel;
			
float Selore_PenetratorEnabled;
float Selore_PenetratorLength;
float3 Selore_StartPosition;
float3 Selore_StartRotation;
float Selore_DeformStrength;
float Selore_AllTheWayThrough;            

float Selore_BezierHandleSize;
float Selore_SplineDebug;

// TODO: see SPS, add blendshape baking feature, as some users may need it.

#endif
