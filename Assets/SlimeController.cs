using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlimeController : MonoBehaviour
{
    [Header("Simulation")]
    public int AgentCount = 10;
    public float MoveSpeed = 10;
    public float EvaporateSpeed = 1;

    [Header("Canvas Size")]
    public int Width = 320;
    public int Height = 200;

    [Header("References")]
    public ComputeShader Shader;
    public RawImage Image;

    // Agent structure
    private struct Agent
    {
        public Vector2 Position;
        public float Angle;
        public const int Size = sizeof(float) * 3;
    }

    // Private fields
    private int MoveKernel;
    private int EvaporateKernel;
    private ComputeBuffer Buffer;
    private Agent[] Agents;

    private DoubleRenderBuffer Textures;

    // Start is called before the first frame update
    void Start()
    {
        // Getting the kernel handle
        MoveKernel = Shader.FindKernel("Move");
        EvaporateKernel = Shader.FindKernel("Evaporate");

        // Creating the texture
        Textures = new DoubleRenderBuffer(Width, Height);

        // Creating the agent buffer
        Buffer = new ComputeBuffer(AgentCount, Agent.Size);
        Agents = new Agent[AgentCount];
        
        Vector2 center = new Vector2(Width / 2, Height / 2);

        for (int i = 0; i < AgentCount; i++)
        {
            // The agents will be centered on the screen with a random angle
            Agents[i] = new Agent()
            {
                Position = center,
                Angle = Random.Range(0f, Mathf.PI * 2f)
            };
        }

        Buffer.SetData(Agents);

        // Assinging the buffer and texture to the shader
        Shader.SetBuffer(MoveKernel, "Agents", Buffer);
    }

    // Update is called once per frame
    void Update()
    {
        // Setting the values
        Shader.SetInt("width", Width);
        Shader.SetInt("height", Height);
        Shader.SetInt("agentCount", AgentCount);
        Shader.SetFloat("moveSpeed", MoveSpeed);
        Shader.SetFloat("evaporateSpeed", EvaporateSpeed);
        Shader.SetFloat("deltaTime", Time.deltaTime);

        // Agent move pass
        Shader.SetTexture(MoveKernel, "Result", Textures.Write);
        Shader.Dispatch(MoveKernel, Mathf.CeilToInt(AgentCount / 16f), 1, 1);

        // Evaporate pass
        Shader.SetTexture(EvaporateKernel, "Result", Textures.Write);
        Shader.SetTexture(EvaporateKernel, "Evaporated", Textures.Read);
        Shader.Dispatch(EvaporateKernel, Mathf.CeilToInt(Width / 8f), Mathf.CeilToInt(Height / 8f), 1);

        // Swapping the textures, so that the processed texture becomes the input for the next frame
        Textures.Swap();

        Image.texture = Textures.Read;
    }

    private void OnDestroy()
    {
        Buffer.Dispose();
        Textures.Dispose();
    }
}
