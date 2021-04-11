Shader "Custom/RTEEllipseShader"
{
  Properties
  {
    _EyePosition("Eye Position", Vector) = (0, 0, 0, 0)
    _RadiusX("Radius X", float) = 1
    _RadiusY("Radius X", float) = 1
    _RadiusZ("Radius X", float) = 1
    _DeltaPhi("Delta Phi", float) = 20
    _DeltaTheta("Delta Theta", float) = 20
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
      float _DeltaTheta;
      float3 _OneOverEllipsoidRadiiSquared;
      float3 _EyePosition;
      float4x4 _MVPRTE;

      float3 GeodeticSurfaceNormal(float3 positionOnEllipsoid)
      {
        return normalize(positionOnEllipsoid * _OneOverEllipsoidRadiiSquared);
      }

      float2 ComputeTextureCoordinates(float3 normal)
      {
        return float2(
          atan2(normal.z,normal.x) * OneOverTwoPi + 0.5,
          asin(normal.y) * OneOverPi + 0.5);
      }

      struct appdata
      {
        float3 RTEPos : POSITION; // eye-relative ellipse center position
      };

      struct vert2geom
      {
        float4 RTEPos : SV_POSITION; //global position
      };

      struct geom2frag
      {
        float4 pos : SV_POSITION;
        fixed3 color : COLOR;
      };

      vert2geom vert(appdata i)
      {
        vert2geom o;

        o.RTEPos = float4(i.RTEPos, 1);

        return o;
      }

      [maxvertexcount(146)]
      void geom(point vert2geom i[1], inout TriangleStream<geom2frag> tristream)
      {
        geom2frag o;

        //o.pos = UnityObjectToClipPos(i[0].RTEPos + _EyePosition + float3(0, 1, 0));
        //tristream.Append(o);
        //o.pos = UnityObjectToClipPos(i[0].RTEPos + _EyePosition + float3(1, 0, 0));
        //tristream.Append(o);
        //o.pos = UnityObjectToClipPos(i[0].RTEPos + _EyePosition + float3(-1, 0, 0));
        //tristream.Append(o);
        //tristream.RestartStrip();
        float3 globalPos = i[0].RTEPos + _EyePosition;

        for (float i = 0; i < TwoPi; i += _DeltaTheta)
        {
          for (float j = 0; j < PI - _DeltaPhi; j += _DeltaPhi)
          {
            o.pos = UnityObjectToClipPos(
              float3(
              (_RadiusX * cos(i + _DeltaTheta) * sin(j)) + globalPos.x
                , (_RadiusY * cos(j)) + globalPos.y
                , (_RadiusZ * sin(i + _DeltaTheta) * sin(j)) + globalPos.z));
            o.color = Green;
            tristream.Append(o);

            o.pos = UnityObjectToClipPos(
              float3(
              (_RadiusX * cos(i + _DeltaTheta) * sin(j + _DeltaPhi)) + globalPos.x
                , (_RadiusY * cos(j + _DeltaPhi)) + globalPos.y
                , (_RadiusZ * sin(i + _DeltaTheta) * sin(j + _DeltaPhi)) + globalPos.z));
            o.color = Blue;
            tristream.Append(o);

            o.pos = UnityObjectToClipPos(
              float3(
                (_RadiusX * cos(i) * sin(j)) + globalPos.x
                , (_RadiusY * cos(j)) + globalPos.y
                , (_RadiusZ * sin(i) * sin(j)) + globalPos.z));
            o.color = Red;
            tristream.Append(o);
            
            o.pos = UnityObjectToClipPos(
              float3(
              (_RadiusX * cos(i + _DeltaTheta) * sin(j + _DeltaPhi)) + globalPos.x
                , (_RadiusY * cos(j + _DeltaPhi)) + globalPos.y
                , (_RadiusZ * sin(i + _DeltaTheta) * sin(j + _DeltaPhi)) + globalPos.z));
            o.color = Blue;
            tristream.Append(o);

            o.pos = UnityObjectToClipPos(
              float3(
              (_RadiusX * cos(i) * sin(j + _DeltaPhi)) + globalPos.x
                , (_RadiusY * cos(j + _DeltaPhi)) + globalPos.y
                , (_RadiusZ * sin(i) * sin(j + _DeltaPhi)) + globalPos.z));
            o.color = Purple;
            tristream.Append(o);
            
            tristream.RestartStrip();
          }
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
