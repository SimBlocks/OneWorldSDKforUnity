//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
﻿using System;
using UnityEngine;

namespace sbio.OneWorldSDKViewer
{
  [Serializable]
  public struct FeatureAttribute
  {
    public string Name;
    public string Value;

    public FeatureAttribute(string name, string value)
    {
      Name = name;
      Value = value;
    }
  }

  public sealed class FeatureObjectInfo : MonoBehaviour
  {
    #region MonoBehaviour

    public Material SelectedMaterial;
    public FeatureAttribute[] Attributes;

    private void Awake()
    {
      m_Renderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
      if (m_Selected)
      {
        SelectImpl();
      }
    }

    private void OnDisable()
    {
      if (m_Selected)
      {
        DeselectImpl();
      }
    }

    #endregion

    public void Select()
    {
      if (m_Selected)
      {
        throw new InvalidOperationException("Already selected");
      }

      SelectImpl();

      m_Selected = true;
    }

    public void Deselect()
    {
      if (!m_Selected)
      {
        throw new InvalidOperationException();
      }

      DeselectImpl();

      m_Selected = false;
    }

    private void SelectImpl()
    {
      if (m_Renderer != null)
      {
        m_PrevMaterial = m_Renderer.material;
        m_Renderer.material = SelectedMaterial;
      }
      else
      {
        m_PrevMaterial = null;
      }
    }

    private void DeselectImpl()
    {
      if (m_Renderer != null)
      {
        m_Renderer.material = m_PrevMaterial;
        m_PrevMaterial = null;
      }
    }

    private Material m_PrevMaterial;
    private Renderer m_Renderer;
    private bool m_Selected;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
