//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

namespace sbio.owsdk.Config
{
  public enum ConfigValueType
  {
    Null,
    Boolean,
    Integer,
    Number,
    String,
    Object,
    Array
  }

  public interface IConfigValue
  {
    ConfigValueType ValueType { get; }

    bool BoolValue { get; }
    int IntValue { get; }
    double NumberValue { get; }
    string StringValue { get; }
    IConfigObject ObjectValue { get; }
    IConfigArray ArrayValue { get; }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
