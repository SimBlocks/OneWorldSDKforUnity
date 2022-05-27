//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
namespace sbio.owsdk.Features
{
  public struct FeatureRelationMember
  {
    public string Role { get; }
    public Feature Feature { get; }

    public FeatureRelationMember(string role, Feature feature)
    {
      Role = role;
      Feature = feature;
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
