using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using UnityEngine;

public class ModelModuleTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var playerModel = GlobalModule.ModelModule.GetModel<PlayerModel>();
        FuLogger.LogInfo($"Level1: {playerModel.Level}");

        playerModel.Level = 10;
        FuLogger.LogInfo($"Level2: {playerModel.Level}");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FuFramework.Entry.Runtime.Launcher.Restart();
        }
    }
}