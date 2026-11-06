// Debug Mode's picking and overlay shader (see DebugMode.cs). Lives in Resources so builds that carry Debug Mode
// (the editor and development builds) can load it.
Shader "Hidden/CloneZ/DebugPick"
{
    Properties
    {
        _Color ("Overlay colour", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        // Pass 0: picking. Every renderer drawn flat in its id colour (_DebugPickId, set before each draw) into
        // target 0, and the world position of each pixel into target 1. _DebugPickVP is the GPU view-projection of
        // the small patch round the cursor (set directly, so no matrix conventions are guessed at).
        Pass
        {
            Name "Pick"
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _DebugPickId;
            float4x4 _DebugPickVP;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; };
            struct targets { float4 id : SV_Target0; float4 world : SV_Target1; };

            v2f vert (appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));
                o.pos = mul(_DebugPickVP, world);
                o.world = world.xyz;
                return o;
            }

            targets frag (v2f i)
            {
                targets o;
                o.id = _DebugPickId;
                o.world = float4(i.world, 1);
                return o;
            }
            ENDCG
        }

        // Pass 1: overlay lines (bounds boxes), drawn over everything in their vertex colour.
        Pass
        {
            Name "Overlay"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return i.color; }
            ENDCG
        }
    }
}
