//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;

namespace sbio.owsdk.Services
{
  public struct TileMesh
  {
    public Vec3LeftHandedGeocentric Center
    {
      get { return m_Center; }
    }

    public Vector3f Extents
    {
      get { return m_Extents; }
    }

    public QuaternionLeftHandedGeocentric Rotation
    {
      get { return m_Rotation; }
    }

    public Vector3f[] Vertices
    {
      get { return m_Vertices; }
    }

    public Vector2f[] Uvs
    {
      get { return m_Uvs; }
    }

    public Vector3f[] Normals
    {
      get { return m_Normals; }
    }

    public int[] Triangles
    {
      get { return m_Triangles; }
    }

    public TileMesh(Vec3LeftHandedGeocentric center, Vector3f extents, QuaternionLeftHandedGeocentric rotation, Vector3f[] vertices, Vector2f[] uvs, Vector3f[] normals, int[] triangles)
    {
      m_Center = center;
      m_Extents = extents;
      m_Rotation = rotation;
      m_Vertices = vertices;
      m_Uvs = uvs;
      m_Normals = normals;
      m_Triangles = triangles;
    }

    private readonly Vec3LeftHandedGeocentric m_Center;
    private readonly Vector3f m_Extents;
    private readonly QuaternionLeftHandedGeocentric m_Rotation;
    private readonly Vector3f[] m_Vertices;
    private readonly Vector2f[] m_Uvs;
    private readonly Vector3f[] m_Normals;
    private readonly int[] m_Triangles;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
