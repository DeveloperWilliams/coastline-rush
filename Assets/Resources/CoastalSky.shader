Shader "Coastline/Coastal Sky" {
 Properties { _Top("Zenith",Color)=(.08,.27,.40,1) _Horizon("Horizon",Color)=(1,.68,.4,1) _SunDirection("Sun",Vector)=(.3,.18,-.8,0) }
 SubShader { Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;};struct v2f {float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
 float4 _Top,_Horizon;float4 _SunDirection;
 v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target{float3 d=normalize(i.direction);float t=pow(saturate(d.y),.45);float3 color=lerp(_Horizon.rgb,_Top.rgb,t);float sun=pow(saturate(dot(d,normalize(_SunDirection.xyz))),900);float glow=pow(saturate(dot(d,normalize(_SunDirection.xyz))),12);color+=float3(1,.63,.26)*(sun*2+glow*.16);return float4(color,1);}
 ENDCG }
 }
 Fallback Off
}
