//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

#if UNITY_EDITOR
using sbio.Core.Math;
using UnityEngine;
using UnityEditor;

namespace sbio.owsdk.Unity.Events.Editor
{
  [CustomEditor(typeof(GameEvent))]
  public class GameEventEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      GUI.enabled = Application.isPlaying;

      EditorGUILayout.Space();

      if (GUILayout.Button("Raise"))
      {
        ((GameEvent)target).Raise();
      }
    }
  }

  [CustomEditor(typeof(BoolEvent))]
  public class BoolEventEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      GUI.enabled = Application.isPlaying;

      m_Data = EditorGUILayout.Toggle("Data", m_Data);

      EditorGUILayout.Space();

      if (GUILayout.Button("Raise"))
      {
        ((BoolEvent)target).Raise(m_Data);
      }
    }

    private bool m_Data;
  }

  [CustomEditor(typeof(IntEvent))]
  public class IntEventEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      GUI.enabled = Application.isPlaying;

      m_Data = EditorGUILayout.IntField("Data", m_Data);

      EditorGUILayout.Space();

      if (GUILayout.Button("Raise"))
      {
        ((IntEvent)target).Raise(m_Data);
      }
    }

    private int m_Data;
  }


  [CustomEditor(typeof(FloatEvent))]
  public class FloatEventEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      GUI.enabled = Application.isPlaying;

      m_Data = EditorGUILayout.FloatField("Data", m_Data);

      EditorGUILayout.Space();

      if (GUILayout.Button("Raise"))
      {
        ((FloatEvent)target).Raise(m_Data);
      }
    }

    private float m_Data;
  }


  [CustomEditor(typeof(DoubleEvent))]
  public class DoubleEventEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      GUI.enabled = Application.isPlaying;

      m_Data = EditorGUILayout.DoubleField("Data", m_Data);

      EditorGUILayout.Space();

      if (GUILayout.Button("Raise"))
      {
        ((DoubleEvent)target).Raise(m_Data);
      }
    }

    private double m_Data;
  }


  [CustomEditor(typeof(StringEvent))]
  public class StringEventEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      GUI.enabled = Application.isPlaying;

      m_Data = EditorGUILayout.TextField("Data", m_Data);

      EditorGUILayout.Space();

      if (GUILayout.Button("Raise"))
      {
        ((StringEvent)target).Raise(m_Data);
      }
    }

    private string m_Data;
  }

  [CustomEditor(typeof(Vector3dEvent))]
  public class Vector3dEventEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      GUI.enabled = Application.isPlaying;

      EditorGUILayout.LabelField("Data", GUILayout.MinWidth(25));
      EditorGUILayout.Space();
      ++EditorGUI.indentLevel;
      EditorGUILayout.BeginHorizontal();
      var x = EditorGUILayout.DoubleField(m_Data.X);
      var y = EditorGUILayout.DoubleField(m_Data.Y);
      var z = EditorGUILayout.DoubleField(m_Data.Z);
      m_Data = new Vec3LeftHandedGeocentric(x, y, z);
      EditorGUILayout.EndHorizontal();
      --EditorGUI.indentLevel;

      EditorGUILayout.Space();

      if (GUILayout.Button("Raise"))
      {
        ((Vector3dEvent)target).Raise(m_Data);
      }
    }

    private Vec3LeftHandedGeocentric m_Data;
  }
}
#endif



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
