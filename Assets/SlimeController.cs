using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UI;

public class SlimeController : MonoBehaviour
{
    [Header("Simulation")]
    public int AgentCount = 600;
    public float MoveSpeed = 60;
    public float TurnSpeed = 2;
    public float DiffuseSpeed = 15;
    public float EvaporateStrength = 0.8f;

    [Header("Sensor")]
    public float SensorSpacingRad = 0.25f;
    public float SensorOffsetDistance = 5;
    public int SensorSize = 1;

    [Header("Styles")]
    public Color HighColor = Color.red;
    public Color LowColor = Color.green;

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
    private int ProcessKernel;
    private int ColorizeKernel;
    private ComputeBuffer Buffer;
    private Agent[] Agents;

    private DoubleRenderBuffer Textures;
    private RenderTexture DisplayTexture;

    // Start is called before the first frame update
    void Start()
    {
        // Getting the kernel handle
        MoveKernel = Shader.FindKernel("Move");
        ProcessKernel = Shader.FindKernel("Process");
        ColorizeKernel = Shader.FindKernel("Colorize");

        // Creating the textures
        Textures = new DoubleRenderBuffer(Width, Height);
        DisplayTexture = CreateTexture();

        // Creating the agent buffer
        Buffer = new ComputeBuffer(AgentCount, Agent.Size);
        Agents = new Agent[AgentCount];

        Vector2 center = new Vector2(Width / 2f, Height / 2f);
        float radius = Height / 2f;

        for (int i = 0; i < AgentCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float r = radius * Mathf.Sqrt(Random.value);

            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 pos = center + dir * r;

            // Face outward or inward
            float facingAngle = (i % 2 == 0) ? angle + Mathf.PI : angle;

            Agents[i] = new Agent()
            {
                Position = pos,
                Angle = facingAngle
            };
        }

        Buffer.SetData(Agents);

        // Assinging the buffer and texture to the shader
        Shader.SetBuffer(MoveKernel, "Agents", Buffer);
        Shader.SetTexture(ColorizeKernel, "Colorized", DisplayTexture);
    }

    // Update is called once per frame
    void Update()
    {
        // Setting the values
        Shader.SetInt("width", Width);
        Shader.SetInt("height", Height);
        Shader.SetInt("agentCount", AgentCount);
        Shader.SetInt("sensorSize", SensorSize);
        Shader.SetFloat("moveSpeed", MoveSpeed);
        Shader.SetFloat("diffuseSpeed", DiffuseSpeed);
        Shader.SetFloat("turnSpeed", TurnSpeed);
        Shader.SetFloat("evaporateStrength", EvaporateStrength);
        Shader.SetFloat("sensorSpacing", SensorSpacingRad);
        Shader.SetFloat("sensorOffsetDistance", SensorOffsetDistance);
        Shader.SetFloat("deltaTime", Time.deltaTime);
        Shader.SetVector("highColor", ColorToVec(HighColor));
        Shader.SetVector("lowColor", ColorToVec(LowColor));

        // Agent move pass
        Shader.SetTexture(MoveKernel, "Result", Textures.Read);
        Shader.Dispatch(MoveKernel, Mathf.CeilToInt(AgentCount / 16f), 1, 1);

        // Evaporate pass
        Shader.SetTexture(ProcessKernel, "Result", Textures.Read);
        Shader.SetTexture(ProcessKernel, "Processed", Textures.Write);
        Shader.Dispatch(ProcessKernel, Mathf.CeilToInt(Width / 8f), Mathf.CeilToInt(Height / 8f), 1);

        // Color pass
        Shader.SetTexture(ColorizeKernel, "Processed", Textures.Write);
        Shader.Dispatch(ColorizeKernel, Mathf.CeilToInt(Width / 8f), Mathf.CeilToInt(Height / 8f), 1);

        // Swapping the textures, so that the processed texture becomes the input for the next frame
        Textures.Swap();

        Image.texture = DisplayTexture;
    }

    private void OnDestroy()
    {
        Buffer.Dispose();
        Textures.Dispose();
    }

    private static Vector4 ColorToVec(Color color) => new Vector4(color.r, color.g, color.b, color.a);

    private RenderTexture CreateTexture()
    {
        RenderTexture texture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat);
        texture.filterMode = FilterMode.Point;
        texture.enableRandomWrite = true;
        texture.Create();

        return texture;
    }
}
