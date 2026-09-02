using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core;
using MonoGameLearning.Core.Audio;

namespace MonoGameLearning.Game.GameLoop;

public static class GameLoopRules
{
    private const float SPAWN_BUFFER_X = 60f;
    private const float LEVEL_EDGE_BUFFER = 10f;

    public static bool TryConsumeLife(ref int lives)
    {
        if (lives <= 0) return false;
        lives--;
        return true;
    }

    public static void ApplyMusicForState(AudioService audio, GameState previous, GameState current)
    {
        if (previous == GameState.Paused && current != GameState.Paused)
            audio.SetPaused(false);

        switch (current)
        {
            case GameState.TitleScreen:
            case GameState.Settings:
                audio.PlayMusic(MusicId.TitleMenu);
                break;
            case GameState.Playing:
                audio.PlayMusic(MusicId.Gameplay);
                break;
            case GameState.LevelComplete:
                audio.PlayMusic(MusicId.LevelComplete);
                break;
            case GameState.Paused:
                audio.SetPaused(true);
                break;
            case GameState.GameOver:
                audio.PlayMusic(null);
                break;
        }
    }

    public static Vector2 ComputeRespawnPosition(float cameraX, RectangleF movementBounds, float walkableTopY)
    {
        float levelLeft = movementBounds.X;
        float levelRight = movementBounds.Right;
        float desiredX = cameraX + SPAWN_BUFFER_X;
        float clampedX = Math.Clamp(desiredX, levelLeft + LEVEL_EDGE_BUFFER, levelRight - LEVEL_EDGE_BUFFER);
        System.Diagnostics.Debug.Assert(clampedX >= levelLeft + LEVEL_EDGE_BUFFER,
            "Respawn X clamped below level-left buffer — camera is before level start?");
        return new Vector2(clampedX, walkableTopY);
    }
}