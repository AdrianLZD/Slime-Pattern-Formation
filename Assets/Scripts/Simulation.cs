using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour {

    public ComputeShader simulationShader;
    public ComputeShader agentShader;
    public RenderTexture targetTexture;
    public RenderTexture trailTexture;
    public RenderTexture diffusedTrailTexture;
    public int agentsAmount = 100;

    public int width = 256;
    public int height = 256;

    [Header("Agents Settings")]
    public float moveSpeed;
    public float turnSpeed;
    public float sensorAngleDegrees;
    public float sensorOffset;
    public int sensorSize;

    private ComputeBuffer agentsBuffer;
    private ComputeBuffer settingsBuffer;
    private int agentKernel;
    private int renderKernel;
    private int clearTextureKernel;
    private int diffuseMapKernel;

    private Agent[] agents;    

    private void Start(){
        CreateTextures();
        CreateAgents();
        agentKernel = agentShader.FindKernel("AgentUpdate");
        renderKernel = simulationShader.FindKernel("RenderSimulation");
        clearTextureKernel = simulationShader.FindKernel("ClearTexture");
    }

    private void CreateTextures(){
        targetTexture = new RenderTexture(width, height, 24);
        targetTexture.enableRandomWrite = true;
        targetTexture.Create();

        trailTexture = new RenderTexture(width, height, 24);
        trailTexture.enableRandomWrite = true;
        trailTexture.Create();

        diffusedTrailTexture = new RenderTexture(width, height, 24);
        diffusedTrailTexture.enableRandomWrite = true;
        diffusedTrailTexture.Create();
    }

    private void CreateAgents(){
        agents = new Agent[agentsAmount];
        Vector2 centre = new Vector2(width/2, height/2);
        Vector2 defaultPos = Vector2.zero;

        for(int i = 0; i < agentsAmount; i++){
            Vector2 startPos = new Vector2(Random.Range(0, width), Random.Range(0, height));
            Color color = new Color(Random.value, Random.value, Random.value, 1);
            float randomAngle = Random.value * Mathf.PI * 2;
            agents[i] = new Agent(){angle = randomAngle, position = startPos, color = color};
        }

        agentsBuffer = new ComputeBuffer(agents.Length, Agent.size);
        agentsBuffer.SetData(agents);

        simulationShader.SetBuffer(0, "agents", agentsBuffer);
        simulationShader.SetInt("agentsAmount", agentsAmount);
        agentShader.SetBuffer(0, "agents", agentsBuffer);        
    }

    
    private void FixedUpdate() {
        var settings = new AgentsSettings[1];
        settings[0] = new AgentsSettings{
            moveSpeed = moveSpeed, 
            turnSpeed = turnSpeed, 
            sensorAngleDegrees = sensorAngleDegrees, 
            sensorOffset = sensorOffset, 
            sensorSize = sensorSize
        };

        settingsBuffer = new ComputeBuffer(agentsAmount, AgentsSettings.size);
        settingsBuffer.SetData(settings);

        agentShader.SetBuffer(agentKernel, "agentsSettings", settingsBuffer);
        agentShader.SetTexture(agentKernel, "TrailMap", trailTexture);
        agentShader.SetInt("width", width);
        agentShader.SetInt("height", height);
        agentShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        agentShader.SetFloat("time", Time.fixedTime);
        agentShader.Dispatch(agentKernel, agentsAmount, 1, 1);
        
    }

    private void LateUpdate(){
        simulationShader.SetInt("width", width);
        simulationShader.SetInt("height", height);
        simulationShader.SetTexture(clearTextureKernel, "Source", targetTexture);
        simulationShader.Dispatch(clearTextureKernel, width, height, 1);

        simulationShader.SetTexture(renderKernel, "TargetTexture", targetTexture);
        simulationShader.Dispatch(renderKernel, agentsAmount, 1, 1);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // Read pixels from the source RenderTexture, apply the material, copy the updated results to the destination RenderTexture
        Graphics.Blit(targetTexture, dest);
    }

    private void OnDestroy(){
        agentsBuffer.Dispose();
    }
}

public struct Agent {
    public float angle;
    public Vector2 position;
    public Color color;

    public static int size = sizeof(float) * 7;
}

public struct AgentsSettings {
    public float moveSpeed;
    public float turnSpeed;

    public float sensorAngleDegrees;
    public float sensorOffset;

    public int sensorSize;

    public static int size = sizeof(float) * 4 + sizeof(int);
}


