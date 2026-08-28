using MonoGameLearning.Core.Audio;
using MonoGameLearning.Game.Audio;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class AudioManifestTests
{
    [Test]
    public void SfxAssets_CoversEverySfxIdExactlyOnce()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AudioManifest.SfxAssets, Has.Count.EqualTo(Enum.GetValues<SfxId>().Length));
            Assert.That(AudioManifest.SfxAssets.Select(a => a.Id), Is.EquivalentTo(Enum.GetValues<SfxId>()));
        });
    }

    [Test]
    public void MusicAssets_CoversEveryMusicIdExactlyOnce()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AudioManifest.MusicAssets, Has.Count.EqualTo(Enum.GetValues<MusicId>().Length));
            Assert.That(AudioManifest.MusicAssets.Select(a => a.Id), Is.EquivalentTo(Enum.GetValues<MusicId>()));
        });
    }

    [Test]
    public void SfxAssets_HaveUniqueNonEmptyPaths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AudioManifest.SfxAssets.Select(a => a.Path), Is.Unique);
            Assert.That(AudioManifest.SfxAssets, Has.All.Matches<(SfxId Id, string Path)>(a => !string.IsNullOrWhiteSpace(a.Path)));
        });
    }

    [Test]
    public void MusicAssets_HaveUniqueNonEmptyPaths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AudioManifest.MusicAssets.Select(a => a.Path), Is.Unique);
            Assert.That(AudioManifest.MusicAssets, Has.All.Matches<(MusicId Id, string Path)>(a => !string.IsNullOrWhiteSpace(a.Path)));
        });
    }

    [Test]
    public void SfxAssets_ResolveToBuiltContent()
    {
        Assert.Multiple(() =>
        {
            foreach (var (id, path) in AudioManifest.SfxAssets)
                Assert.That(ResolveContentPath(path), Does.Exist, $"{id} ({path})");
        });
    }

    [Test]
    public void MusicAssets_ResolveToBuiltContent()
    {
        Assert.Multiple(() =>
        {
            foreach (var (id, path) in AudioManifest.MusicAssets)
                Assert.That(ResolveContentPath(path), Does.Exist, $"{id} ({path})");
        });
    }

    private static string ResolveContentPath(string assetPath) =>
        Path.Combine(AppContext.BaseDirectory, "Content", assetPath + ".xnb");
}