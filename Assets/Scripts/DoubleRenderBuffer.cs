using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleRenderBuffer : IDisposable
{
    private RenderTexture[] Buffers = new RenderTexture[2];
    private int Id = 0;
    private int Width;
    private int Height;
    private bool IsDisposed;
    public RenderTexture Read => Buffers[Id];
    public RenderTexture Write => Buffers[1 - Id];

    public DoubleRenderBuffer(int width, int height)
    {
        Width = width;
        Height = height;

        // Creating the buffers
        for (int i = 0; i < 2; ++i)
        {
            Buffers[i] = CreateTexture();
            ClearBuffer(Buffers[i]);
        }
    }

    // Swaps the buffers
    public void Swap() => Id ^= 1;

    public void Dispose()
    {
        if (IsDisposed) return;

        foreach (var buffer in Buffers)
        {
            if (buffer != null && buffer.IsCreated())
            {
                buffer.Release();
                UnityEngine.Object.Destroy(buffer);
            }
        }

        IsDisposed = true;
    }

    private void ClearBuffer(RenderTexture texture)
    {
        Graphics.SetRenderTarget(texture);
        GL.Clear(false, true, Color.black);
        Graphics.SetRenderTarget(null);
    }

    private RenderTexture CreateTexture()
    {
        RenderTexture texture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat);
        texture.filterMode = FilterMode.Point;
        texture.enableRandomWrite = true;
        texture.Create();

        return texture;
    }
}
