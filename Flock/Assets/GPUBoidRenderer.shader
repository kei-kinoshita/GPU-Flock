Shader "SPATIALFLOW/BoidRenderer" 
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
		int neighbors : TEXCOORD0;
	};


	struct Boid
	{
		float3 position;
		float3 velocity;
		float3 acceleration;
		int numNeighbors;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	StructuredBuffer<Boid> BoidBuffer;
			
	v2f vert (uint id : SV_VertexID)
	{
		v2f o;
		float4 _pos = float4(BoidBuffer[id].position, 1);
		
		o.pos = UnityObjectToClipPos(_pos);
		o.neighbors = BoidBuffer[id].numNeighbors;
		return o;
	}
			
	float4 frag (v2f i) : SV_Target
	{
		//return float4(1, 1, 1, 1);
		return float4(1,0.5,0.5,1)*smoothstep(0,1000,i.neighbors) + float4(0.5,0.7,1,1)*smoothstep(1000,0,i.neighbors);
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue" = "Geometry"}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
	
	
}