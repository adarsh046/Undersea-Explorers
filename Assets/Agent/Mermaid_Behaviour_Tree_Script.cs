// ECS7016P - Interactive Agents and Procedural Generation
// Coursework: Unity Project (Undersea Explorers)
// Adarsh Gupta - 220570653
// References:
// 1) Meniku/NPBehave: Event Driven Behavior Trees for Unity 3D, GitHub. Available at: https://github.com/meniku/NPBehave
// 2) Sturdyspoon/Unity-movement-ai: A unity library for common movement AI, GitHub. Available at: https://github.com/sturdyspoon/unity-movement-ai 
// 3) Scripts of Lab 1: Behaviour Trees and Lab 5: Steering Behaviours

using UnityEngine;
using NPBehave;
using UnityMovementAI;

public class Mermaid_Behaviour_Tree_Script : MonoBehaviour
{
    public Transform treasure;      // Treasure's transform
    public Transform mine;          // Mine's transform
    public Transform diver;         // Diver's transform
    private Root tree;              // Mermaid's behaviour tree
    private Blackboard blackboard;  // Mermaid's behaviour blackboard

    SteeringBasics steeringBasics;  // Steering basics component
    Flee flee;                      // Flee component
    Wander2 wander;                 // Wander2 component

    void Start()
    {
        steeringBasics = GetComponent<SteeringBasics>();
        flee = GetComponent<Flee>();
        wander = GetComponent<Wander2>();
        tree = CreateBehaviourTree();
        blackboard = tree.Blackboard;
        tree.Start();
    }

    private Root CreateBehaviourTree()
    {
        return Mermaid();
    }

    private void UpdatePerception()
    {
        // Getting the position of the treasure, the diver and the mine
        Vector3 treasurePos = treasure.position;
        Vector3 minePos = mine.position;
        Vector3 diverPos = diver.position;

        // Getting the position of the diver, treasure and the mine in the local coordinate system of the agent
        Vector3 localToTreasurePos = transform.InverseTransformPoint(treasurePos);
        Vector3 localToMinePos = transform.InverseTransformPoint(minePos);
        Vector3 localToDiverPos = transform.InverseTransformPoint(diverPos);

        // Storing the distance between the mermaid and the treasure in the blackboard
        blackboard["treasureDistance"] = localToTreasurePos.magnitude;

        // Storing the distance between the mermaid and the mine in the blackboard
        blackboard["mineDistance"] = localToMinePos.magnitude;

        // Storing the distance between the mermaid and the diver in the blackboard
        blackboard["diverDistance"] = localToDiverPos.magnitude;
    }

    private void destroyTreasure()
    {
        // Teleport the treasure at some random position, so it looks like mermaid is hiding the treasure
        Vector3 newPosition = new Vector3(UnityEngine.Random.Range(10f, 55f), 0.0f, UnityEngine.Random.Range(20f, 100f));
        treasure.transform.position = newPosition;
    }

    private Node treasureDestroy()
    {
        return new Action(() => destroyTreasure());
    }

    private void destroyDiver()
    {
        // Teleport mermaid to some random position, so it looks like mermaid was killed by the diver
        Vector3 newPosition = new Vector3(UnityEngine.Random.Range(10f, 55f), 2.2f, UnityEngine.Random.Range(20f, 100f));
        transform.position = newPosition;
    }

    private Node diverDestroy()
    {
        return new Action(() => destroyDiver());
    }

    private void destroyMine()
    {
        // Teleport mermaid at some random position, so it looks like she has been killed and respawned
        Vector3 newPosition = new Vector3(UnityEngine.Random.Range(10f, 55f), 2.2f, UnityEngine.Random.Range(20f, 100f));
        transform.position = newPosition;

        // Teleport mine at some random position, so it looks like mine has exploded and respawned
        Vector3 newMinePosition = new Vector3(UnityEngine.Random.Range(10f, 55f), 0.0f, UnityEngine.Random.Range(20f, 100f));
        mine.transform.position = newMinePosition;
    }

    private Node mineDestroy()
    {
        return new Action(() => destroyMine());
    }

    private void seekTo(Transform gameObject)
    {
        // From SeekUnit.cs
        Vector3 accel = steeringBasics.Seek(gameObject.position);
        steeringBasics.Steer(accel);
        steeringBasics.LookWhereYoureGoing();
    }

    private Node Seek(Transform gameObject)
    {
        return new Action(() => seekTo(gameObject));
    }

    // Behaviour tree for Mermaid agent
    private Root Mermaid()
    {
        return new Root(
        // Creating a new service that calls the UpdatePerception() every frame
        new Service(UpdatePerception,
        // Creating a new selector that chooses between different behaviours based on the conditions in the blackboard
            new Selector(
               new BlackboardCondition("mineDistance", Operator.IS_SMALLER, 2.0f, Stops.IMMEDIATE_RESTART,
               mineDestroy()),
               new BlackboardCondition("diverDistance", Operator.IS_SMALLER, 2.0f, Stops.IMMEDIATE_RESTART,
               diverDestroy()),
               new BlackboardCondition("diverDistance", Operator.IS_SMALLER_OR_EQUAL, flee.panicDist, Stops.IMMEDIATE_RESTART,
               Flee(diver)),
               new BlackboardCondition("treasureDistance", Operator.IS_SMALLER, 2.0f, Stops.IMMEDIATE_RESTART,
               treasureDestroy()),
               new BlackboardCondition("treasureDistance", Operator.IS_SMALLER_OR_EQUAL, 22.0f, Stops.IMMEDIATE_RESTART,
               Seek(treasure)),
               new BlackboardCondition("mineDistance", Operator.IS_SMALLER_OR_EQUAL, 22.0f, Stops.IMMEDIATE_RESTART,
               Seek(mine)),
               new BlackboardCondition("mineDistance", Operator.IS_GREATER, 22.0f, Stops.IMMEDIATE_RESTART,
               Wander())
               )));
    }

    private Node Flee(Transform gameObject)
    {
        return new Action(() => fleeFrom(gameObject));
    }

    private void fleeFrom(Transform gameObject)
    {
        float distanceToObject = Vector3.Distance(transform.position, gameObject.position);
        if (distanceToObject <= flee.panicDist)
        {
            // From FleeUnit.cs
            Vector3 accel = flee.GetSteering(gameObject.position);
            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }
        else
        {
            wanderFunction();
        }
    }

    private void wanderFunction()
    {
        // From Wander2Unit.cs
        Vector3 accel = wander.GetSteering();
        steeringBasics.Steer(accel);
        steeringBasics.LookWhereYoureGoing();
    }

    private Node Wander()
    {
        return new Action(() => wanderFunction());
    }

}