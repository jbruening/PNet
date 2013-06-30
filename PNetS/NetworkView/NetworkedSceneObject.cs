using SlimMath;
using System.Text.RegularExpressions;
using PNetS;

/// <summary>
/// Objects that exist in a scene with pre-synchronized network id's
/// </summary>
public class NetworkedSceneObject : Component
{
    /// <summary>
    /// The scene/room Network ID of this item. Should only be one per room
    /// </summary>
    public ushort NetworkID = 0;
    /// <summary>
    /// data for the object
    /// </summary>
    public string ObjectData;
    /// <summary>
    /// type of the object
    /// </summary>
    public string ObjectType;

    /// <summary>
    /// position of the object
    /// </summary>
    public Vector3 position;
    /// <summary>
    /// rotation of the object
    /// </summary>
    public Quaternion rotation;

    static System.Globalization.CultureInfo noCulture;

    /// <summary>
    /// Deserialize a networked scene object from a string
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static NetworkedSceneObject Deserialize(string data)
    {
        NetworkedSceneObject newObject = new NetworkedSceneObject();
        noCulture = System.Globalization.CultureInfo.InvariantCulture;




        var line = Regex.Match(data, "id: ?([0-9]+?) ?;");
        if (line.Success)
        {
            ushort.TryParse(line.Groups[1].Value, out newObject.NetworkID);
        }

        line = Regex.Match(data, "type:([\\s\\S]+?);");
        if (line.Success)
        {
            newObject.ObjectType = line.Groups[1].Value;
        }

        line = Regex.Match(data, "data:([\\s\\S]+?);");
        if (line.Success)
        {
            newObject.ObjectData = line.Groups[1].Value;
        }

        line = Regex.Match(data, @"pos: ?\(?(-?\d+(?:\.\d*)?), ?(-?\d+(?:\.\d*)?), ?(-?\d+(?:\.\d*)?)\)? ?;");
        if (line.Success)
        {
            newObject.position = new Vector3(float.Parse(line.Groups[1].Value, noCulture), float.Parse(line.Groups[2].Value, noCulture), float.Parse(line.Groups[3].Value, noCulture));
        }

        line = Regex.Match(data, @"rot: ?\(?(-?\d+(?:\.\d*)?), ?(-?\d+(?:\.\d*)?), ?(-?\d+(?:\.\d*)?), ?(-?\d+(?:\.\d*)?)\)? ?;");
        if (line != null)
        {
            newObject.rotation = new Quaternion(
                float.Parse(line.Groups[1].Value, noCulture),
                float.Parse(line.Groups[2].Value, noCulture),
                float.Parse(line.Groups[3].Value, noCulture),
                float.Parse(line.Groups[4].Value, noCulture)
                );
        }

        return newObject;
    }
}