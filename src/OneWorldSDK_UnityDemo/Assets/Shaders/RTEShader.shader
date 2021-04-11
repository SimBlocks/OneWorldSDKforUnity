Shader "Custom/RTEShader"
{
  Properties
  {
    _MainTex("Texture", 2D) = "white" { }
  }
    SubShader
  {
    Tags
    {
      "Queue" = "Background"
    }
    Pass
    {
      CGPROGRAM
      #include "UnityCG.cginc"
      #pragma target 3.0
      #pragma vertex vert
      #pragma fragment frag

      static const float PI = 3.1415926535897932384626433832795;
      static const float OneOverPi = 0.31830988618379067153776752674503;
      static const float OneOverTwoPi = 0.15915494309189533576888376337251;
      sampler2D _MainTex;
      float4x4 _MVPRTE;

      float2 ComputeTextureCoordinates(float3 normal)
      {
        return float2(
          atan2(normal.z,normal.x) * OneOverTwoPi + 0.5,
          asin(normal.y) * OneOverPi + 0.5);
      }

      struct appdata
      {
        float3 RTEPos : POSITION; // eye-relative vertex position
        float3 normal : NORMAL;
      };

      struct v2f
      {
        float4 pos : SV_POSITION;
        float3 normal : TEXCOORD0;
      };

      struct f2o
      {
        fixed4 color : COLOR;
      };

      v2f vert(appdata v)
      {
        v2f o;

        o.pos = UnityObjectToClipPos(v.RTEPos);
        o.normal = v.normal;

        return o;
      }

      f2o frag(v2f i)
      {
        f2o o;

        float2 coord = ComputeTextureCoordinates(i.normal);
        o.color = tex2D(_MainTex, coord);

        return o;
      }
      ENDCG
    }
  }
}
