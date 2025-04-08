Shader "Custom/StandardWithDefiningEdges" {
    Properties {
        // Standard PBR properties
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        // Defining edge properties
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _EdgeThickness ("Edge Thickness (px)", Range(0.5, 5)) = 1.0
        _EdgeThreshold ("Edge Angle Threshold (deg)", Range(0,90)) = 30
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 300

        // ------------------------------------------------------
        // Pass 1: Standard Surface Shader Pass (PBR rendering)
        // ------------------------------------------------------
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        sampler2D _MainTex;
        fixed4 _Color;
        half _Metallic;
        half _Glossiness;
        struct Input {
            float2 uv_MainTex;
        };
        void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = tex.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = tex.a;
        }
        ENDCG

        // ------------------------------------------------------
        // Pass 2: Defining Edges Overlay via Geometry Shader
        // ------------------------------------------------------
        Pass {
            Name "DefiningEdges"
            Tags { "LightMode"="Always" }
            Cull Off           // Process both sides to ensure no edge is missed.
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vertEdge
            #pragma geometry geomEdge
            #pragma fragment fragEdge
            #include "UnityCG.cginc"

            // Uniforms for edge overlay.
            uniform float4 _EdgeColor;
            uniform float _EdgeThickness;    // in pixels
            uniform float _EdgeThreshold;    // in degrees

            // _WorldSpaceCameraPos and _ScreenParams are provided by Unity.

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            // Pass world-space position, normal, and clip space position.
            struct v2g {
                float4 clipPos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2g vertEdge(appdata v) {
                v2g o;
                float4 worldPos4 = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos4.xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.clipPos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            // Geometry shader: For each triangle, examine its edges.
            // If an edge is either a silhouette edge or sharp enough (per _EdgeThreshold),
            // output a screen-space quad (two triangles) for that edge.
            [maxvertexcount(12)]
            void geomEdge(triangle v2g input[3], inout TriangleStream<v2g> triStream) {
                // Precompute cosine of the threshold angle.
                float cosThreshold = cos(radians(_EdgeThreshold));

                // Process each of the 3 edges.
                for (int i = 0; i < 3; i++) {
                    int j = (i + 1) % 3;
                    v2g v0 = input[i];
                    v2g v1 = input[j];

                    // Check if the edge is "sharp" by comparing the vertex normals.
                    bool isSharp = (dot(normalize(v0.worldNormal), normalize(v1.worldNormal)) < cosThreshold);

                    // Silhouette test: if either vertex is facing away from the camera.
                    float3 viewDir0 = normalize(_WorldSpaceCameraPos - v0.worldPos);
                    float3 viewDir1 = normalize(_WorldSpaceCameraPos - v1.worldPos);
                    bool isSilhouette = (dot(v0.worldNormal, viewDir0) < 0.0 || dot(v1.worldNormal, viewDir1) < 0.0);

                    if (!(isSharp || isSilhouette))
                        continue; // Skip non-defining edges.

                    // Convert clip-space positions to NDC.
                    float4 clipA = v0.clipPos;
                    float4 clipB = v1.clipPos;
                    float2 ndcA = clipA.xy / clipA.w;
                    float2 ndcB = clipB.xy / clipB.w;
                    // Convert NDC to screen pixel coordinates.
                    float2 screenA = (ndcA * 0.5 + 0.5) * _ScreenParams.xy;
                    float2 screenB = (ndcB * 0.5 + 0.5) * _ScreenParams.xy;

                    // Compute the screen-space edge direction and its perpendicular.
                    float2 edgeDir = normalize(screenB - screenA);
                    float2 perp = float2(-edgeDir.y, edgeDir.x);
                    // Determine offset in pixels.
                    float2 pixelOffset = perp * _EdgeThickness;
                    // Convert pixel offset to NDC.
                    float2 ndcOffset = pixelOffset / _ScreenParams.xy;

                    // Calculate offset positions in NDC.
                    float2 ndcA_plus  = ndcA + ndcOffset;
                    float2 ndcA_minus = ndcA - ndcOffset;
                    float2 ndcB_plus  = ndcB + ndcOffset;
                    float2 ndcB_minus = ndcB - ndcOffset;

                    // Reconstruct clip-space positions (depth preserved by multiplying by original w).
                    float4 posA_plus  = float4(ndcA_plus * clipA.w, clipA.z, clipA.w);
                    float4 posA_minus = float4(ndcA_minus * clipA.w, clipA.z, clipA.w);
                    float4 posB_plus  = float4(ndcB_plus * clipB.w, clipB.z, clipB.w);
                    float4 posB_minus = float4(ndcB_minus * clipB.w, clipB.z, clipB.w);

                    // Emit a quad (two triangles) for the edge.
                    v2g outV;
                    outV.worldPos = float3(0,0,0);      // Not needed in fragment.
                    outV.worldNormal = float3(0,0,0);     // Not needed in fragment.

                    // First triangle: posA_plus, posB_plus, posA_minus.
                    outV.clipPos = posA_plus; triStream.Append(outV);
                    outV.clipPos = posB_plus; triStream.Append(outV);
                    outV.clipPos = posA_minus; triStream.Append(outV);
                    triStream.RestartStrip();

                    // Second triangle: posA_minus, posB_plus, posB_minus.
                    outV.clipPos = posA_minus; triStream.Append(outV);
                    outV.clipPos = posB_plus; triStream.Append(outV);
                    outV.clipPos = posB_minus; triStream.Append(outV);
                    triStream.RestartStrip();
                }
            }

            fixed4 fragEdge(v2g i) : SV_Target {
                return _EdgeColor;
            }
            ENDCG
        }
    }
    FallBack "Standard"
}
