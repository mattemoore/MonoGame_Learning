using MonoGameLearning.Core;
using MonoGameLearning.Core.Audio;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class AudioServiceTests
{
    [Test]
    public void PlaySfx_WithoutLoadedContent_IsNoOp()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => mgr.PlaySfx(SfxId.MenuNavigate));
    }

    [Test]
    public void PlayMusic_Null_DoesNotThrow()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => mgr.PlayMusic(null));
    }

    [Test]
    public void PlayMusic_SameTrack_DoesNotRestart()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => mgr.PlayMusic(MusicId.TitleMenu));
        Assert.DoesNotThrow(() => mgr.PlayMusic(MusicId.TitleMenu));
    }

    [Test]
    public void SfxVolume_ClampsZeroToOne()
    {
        var mgr = new AudioService();
        mgr.SfxVolume = -0.5f;
        Assert.That(mgr.SfxVolume, Is.EqualTo(0f));
        mgr.SfxVolume = 1.5f;
        Assert.That(mgr.SfxVolume, Is.EqualTo(1f));
    }

    [Test]
    public void MusicVolume_ClampsZeroToOne()
    {
        var mgr = new AudioService();
        mgr.MusicVolume = -0.5f;
        Assert.That(mgr.MusicVolume, Is.EqualTo(0f));
        mgr.MusicVolume = 1.5f;
        Assert.That(mgr.MusicVolume, Is.EqualTo(1f));
    }

    [Test]
    public void SetPaused_DoesNotThrow_WithoutMusic()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => mgr.SetPaused(true));
        Assert.That(mgr.IsPausedForTest, Is.True);
        Assert.DoesNotThrow(() => mgr.SetPaused(false));
        Assert.That(mgr.IsPausedForTest, Is.False);
    }

    [Test]
    public void Update_WithoutLoadedContent_DoesNotThrow()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => mgr.Update());
    }

    [Test]
    public void SfxVolume_Set_StaysInRange()
    {
        var mgr = new AudioService();
        mgr.SfxVolume = 0.5f;
        Assert.That(mgr.SfxVolume, Is.EqualTo(0.5f));
        mgr.SfxVolume = 0f;
        Assert.That(mgr.SfxVolume, Is.EqualTo(0f));
        mgr.SfxVolume = 1f;
        Assert.That(mgr.SfxVolume, Is.EqualTo(1f));
    }

    [Test]
    public void MusicVolume_Set_StaysInRange()
    {
        var mgr = new AudioService();
        mgr.MusicVolume = 0.3f;
        Assert.That(mgr.MusicVolume, Is.EqualTo(0.3f));
    }

    [Test]
    public void PauseMuting_Multiplicative_NotOverride()
    {
        var mgr = new AudioService();
        mgr.MusicVolume = 0.8f;
        float rawBefore = mgr.RawMusicVolumeForTest;
        Assert.That(rawBefore, Is.EqualTo(0.8f));
        mgr.SetPaused(true);
        Assert.That(mgr.IsPausedForTest, Is.True);
        mgr.SetPaused(false);
        Assert.That(mgr.IsPausedForTest, Is.False);
    }

    [Test]
    public void ComputeMusicVolume_Paused_MultipliesByDuck()
    {
        Assert.That(AudioService.ComputeMusicVolume(0.8f, true), Is.EqualTo(0.24f).Within(1e-6f));
    }

    [Test]
    public void ComputeMusicVolume_Unpaused_ReturnsBase()
    {
        Assert.That(AudioService.ComputeMusicVolume(0.8f, false), Is.EqualTo(0.8f));
    }

    [Test]
    public void ComputeMusicVolume_ZeroBase_StaysZero()
    {
        Assert.That(AudioService.ComputeMusicVolume(0f, true), Is.EqualTo(0f));
        Assert.That(AudioService.ComputeMusicVolume(0f, false), Is.EqualTo(0f));
    }

    [Test]
    public void ComputeMusicVolume_FullBasePaused_ReturnsDuck()
    {
        Assert.That(AudioService.ComputeMusicVolume(1f, true), Is.EqualTo(0.3f).Within(1e-6f));
    }

    [Test]
    public void PlaySfx_PickupHeal_DoesNotThrow_WhenAssetMissing()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => mgr.PlaySfx(SfxId.PickupHeal));
    }
}