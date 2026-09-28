using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

/* Source Code referenced from: https://www.youtube.com/watch?v=p8e4Kpl9b28*/
public class Projection : MonoBehaviour
{
    private Scene simulationScene;
    private PhysicsScene physicsScene;
    [SerializeField] private Transform obstaclesParent;
    [SerializeField] private LineRenderer line;
    [SerializeField] private int maxPhysicsFrameIterations = 50;

    private void Start()
    {
        CreatePhysicsScene();
    }

    void CreatePhysicsScene()
    {
        simulationScene = SceneManager.CreateScene("Simulation", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        physicsScene = simulationScene.GetPhysicsScene();

        Physics.SyncTransforms();
        
        if (obstaclesParent != null)
        {
            foreach (Transform obj in obstaclesParent)
            {
                var ghostObj = Instantiate(obj.gameObject, obj.transform.position, obj.rotation);
                if (ghostObj.TryGetComponent<Renderer>(out var rend))
                {
                    rend.enabled = false;
                }
                SceneManager.MoveGameObjectToScene(ghostObj, simulationScene);
            }
        }
    }

    public void SimulateTrajectory(Marble marble, Vector3 pos, Vector3 velocity)
    {
        if (marble == null || line == null) return;

        var ghostObj = Instantiate(marble, pos, Quaternion.identity);
        if (ghostObj.TryGetComponent<Renderer>(out var rend))
        {
            rend.enabled = false;
        }
        SceneManager.MoveGameObjectToScene(ghostObj.gameObject, simulationScene);

        ghostObj.Init(velocity, true);

        line.positionCount = maxPhysicsFrameIterations;

        for (int i = 0; i < maxPhysicsFrameIterations; i++)
        {
            physicsScene.Simulate(Time.fixedDeltaTime);
            line.SetPosition(i, ghostObj.transform.position);
        }

        Destroy(ghostObj.gameObject);
    }
}
