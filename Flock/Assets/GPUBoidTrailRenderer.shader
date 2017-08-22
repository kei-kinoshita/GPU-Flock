Shader "SPATIALFLOW/BoidTrailRenderer" 
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE		
	#include "UnityCG.cginc"

	struct v2f
	{
		float4 pos : POSITION;
		int trailID : TEXCOORD0;
		int numNeighbors : TEXCOORD1;
	};


	struct Trail
	{
		float3 position;
		int trailNum;
		int numNeighbors;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	int CURR_TRAIL_NUM;
	int NUM_TRAILS;

	StructuredBuffer<Trail> BoidTrailBuffer;
			
	v2f vert (uint id : SV_VertexID)
	{
		v2f o;

		float4 _pos = float4(BoidTrailBuffer[id].position, 1);
		o.pos = UnityObjectToClipPos(_pos);
		o.trailID = BoidTrailBuffer[id].trailNum;
		o.numNeighbors =BoidTrailBuffer[id].numNeighbors;
		return o;
	}
			
	float4 frag (v2f i) : SV_Target
	{
		float trailLerp = ((float)(NUM_TRAILS-(   (i.trailID+ NUM_TRAILS-CURR_TRAIL_NUM)%NUM_TRAILS)      ))/(float)NUM_TRAILS;
		

		float4 blue = float4(1,1,1,0)*(1-trailLerp) + float4(0,0.3,1,0)*(trailLerp);
		float4 red = float4(1,0,1,0)*(1-trailLerp) + float4(0,0,0.7,0)*(trailLerp);
		//float4 color = red*smoothstep(0,50,i.numNeighbors) + blue*smoothstep(50,0,i.numNeighbors);
		float4 color = blue;
		color.a = (1-trailLerp);
		return color;
		//		Tags { "RenderType"="Transparent" "Queue" = "Geometry"}

	}
	ENDCG

	SubShader
	{
	    Tags {"Queue"="Transparent" "RenderType"="Transparent" "LightMode"="ForwardBase"}

		LOD 100

		Pass
		{

		    
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
	
	
}