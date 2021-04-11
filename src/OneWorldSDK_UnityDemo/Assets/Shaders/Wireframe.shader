Shader "Custom/Wireframe"
{
  Properties
  {
    _WireColor("Wire Color", Color) = (0, 0, 0, 1)
    _BaseColor("Base Color", Color) = (0, 0, 0, 0)
    _WireSmoothing("Wire Smoothing", Float) = 0.5
    _WireThickness("Wire Thickness", Float) = 0.1
    
  }

  SubShader
  {
    Tags
    {
      "Queue" = "Transparent"
      "RenderType" = "Transparent"
    }
    Pass
    {
      Blend SrcAlpha OneMinusSrcAlpha
      CGPROGRAM
      #include "UnityCG.cginc"
      #pragma vertex vert
      #pragma geometry geom
      #pragma fragment frag
      
      #pragma target 4.0

      uniform float4 _WireColor = float4(0.0, 0.0, 0.0, 1.0);
      uniform float4 _BaseColor = float4(0.0, 0.0, 0.0, 0.0);
      uniform float _WireSmoothing = 0.5;
      uniform float _WireThickness = 0.1;
      
      struct appdata
      {
        float4 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };
      
      struct v2g
      {
        float4 projectionSpaceVertex : SV_POSITION;
      };
      
      struct g2f
      {
        float4 projectionSpaceVertex : SV_POSITION;
        float2 distToEdge : TEXCOORD0;
      };
      
      v2g vert(appdata v)
      {
        v2g o;
        o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
        return o;
      }
      
      [maxvertexcount(3)]
      void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
      {      
        g2f o;
        o.projectionSpaceVertex = i[0].projectionSpaceVertex;
        o.distToEdge = float2(1, 0);
        triangleStream.Append(o);
      
        o.projectionSpaceVertex = i[1].projectionSpaceVertex;
        o.distToEdge = float2(0, 1);
        triangleStream.Append(o);
      
        o.projectionSpaceVertex = i[2].projectionSpaceVertex;
        o.distToEdge = float2(0, 0);
        triangleStream.Append(o);
      }
      
      fixed4 frag(g2f i) : SV_Target
      {
        float3 distToEdge;
        distToEdge.xy = i.distToEdge;
        distToEdge.z = 1 - distToEdge.x - distToEdge.y;
        float3 deltas = fwidth(distToEdge);
        float3 smoothing = deltas * _WireSmoothing;
        float3 thickness = deltas * _WireThickness;
        distToEdge = smoothstep(thickness, thickness + smoothing, distToEdge);
        float minDist = min(distToEdge.x, min(distToEdge.y, distToEdge.z));
        return lerp(_WireColor, _BaseColor, minDist);
      }
      ENDCG
    }
  }
}
