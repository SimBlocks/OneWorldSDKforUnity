Shader "Custom/RTEEllipseStripShader"
{
  Properties
  {
    _EyePosition("Eye Position", Vector) = (0, 0, 0, 0)
    _RadiusX("Radius X", float) = 1
    _RadiusY("Radius X", float) = 1
    _RadiusZ("Radius X", float) = 1
    _DeltaPhi("Delta Phi", float) = 0.25
  }
    SubShader
  {
    Pass
  {
    Cull Off
    CGPROGRAM

    #pragma target 4.0
    #pragma vertex vert
    #pragma geometry geom
    #pragma fragment frag

        static const float PI = 3.1415926535897932384626433832795;
      static const float TwoPi = 6.283185307179586476925286766559;
      static const float OneOverPi = 0.31830988618379067153776752674503;
      static const float OneOverTwoPi = 0.15915494309189533576888376337251;
      static const fixed3 Red = fixed3(1, 0, 0);
      static const fixed3 Green = fixed3(0, 1, 0);
      static const fixed3 Blue = fixed3(0, 0, 1);
      static const fixed3 Purple = fixed3(1, 0, 1);
      float _RadiusX;
      float _RadiusY;
      float _RadiusZ;
      float _DeltaPhi;

      struct appdata
      {
        float3 VertexInfo : POSITION;
      };

      struct vert2geom
      {
        float StripLatitude : TESSFACTOR0;
        float StripWidth : TESSFACTOR1;
      };

      struct geom2frag
      {
        float4 pos : SV_POSITION;
        fixed3 color : COLOR;
      };
         
      vert2geom vert(appdata i)
      {
        vert2geom o;

        o.StripLatitude = i.VertexInfo.x;
        o.StripWidth = i.VertexInfo.y;

        return o;
      }

      [maxvertexcount(146)]
      void geom(point vert2geom i[1], inout TriangleStream<geom2frag> tristream)
      {
        geom2frag o;

        float3 globalPos = float3(0, 0, 0);

        for (float j = 0; j < PI; j += _DeltaPhi)
        {
          o.pos = UnityObjectToClipPos(
            float3(
            (_RadiusX * cos(i[0].StripLatitude + i[0].StripWidth) * sin(j)) + globalPos.x
              , (_RadiusY * cos(j)) + globalPos.y
              , (_RadiusZ * sin(i[0].StripLatitude + i[0].StripWidth) * sin(j)) + globalPos.z));
          o.color = Green;
          tristream.Append(o);

          o.pos = UnityObjectToClipPos(
            float3(
            (_RadiusX * cos(i[0].StripLatitude + i[0].StripWidth) * sin(j + _DeltaPhi)) + globalPos.x
              , (_RadiusY * cos(j + _DeltaPhi)) + globalPos.y
              , (_RadiusZ * sin(i[0].StripLatitude + i[0].StripWidth) * sin(j + _DeltaPhi)) + globalPos.z));
          o.color = Blue;
          tristream.Append(o);

          o.pos = UnityObjectToClipPos(
            float3(
            (_RadiusX * cos(i[0].StripLatitude) * sin(j)) + globalPos.x
              , (_RadiusY * cos(j)) + globalPos.y
              , (_RadiusZ * sin(i[0].StripLatitude) * sin(j)) + globalPos.z));
          o.color = Red;
          tristream.Append(o);

          o.pos = UnityObjectToClipPos(
            float3(
            (_RadiusX * cos(i[0].StripLatitude + i[0].StripWidth) * sin(j + _DeltaPhi)) + globalPos.x
              , (_RadiusY * cos(j + _DeltaPhi)) + globalPos.y
              , (_RadiusZ * sin(i[0].StripLatitude + i[0].StripWidth) * sin(j + _DeltaPhi)) + globalPos.z));
          o.color = Blue;
          tristream.Append(o);

          o.pos = UnityObjectToClipPos(
            float3(
            (_RadiusX * cos(i[0].StripLatitude) * sin(j + _DeltaPhi)) + globalPos.x
              , (_RadiusY * cos(j + _DeltaPhi)) + globalPos.y
              , (_RadiusZ * sin(i[0].StripLatitude) * sin(j + _DeltaPhi)) + globalPos.z));
          o.color = Purple;
          tristream.Append(o);

          tristream.RestartStrip();
        }

      }

      fixed4 frag(geom2frag i) : SV_Target
      {
        return fixed4(i.color, 0);
      }
        ENDCG
      }
  }
}
