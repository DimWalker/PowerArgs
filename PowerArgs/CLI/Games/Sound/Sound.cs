﻿using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Threading.Tasks;

namespace PowerArgs.Games
{
    /// <summary>
    /// The main API for playing sound in PowerArgs games. By default it is configured with a No op provider that will not play any sound.
    /// 
    /// If you want sound and are on Windows then you must make your project a ull .NET project that can reference WindowsBase, PresentationCore, and PresentationFramework.
    /// Then you can use the WindowsSoundProvider that implements ISoundProvider by creating an instance of WindowsSoundProvider.SoundProvider and assigning it to Sound.Provider.
    /// 
    /// I have not yet figured out how to make sound work in .NET Core.
    /// 
    /// </summary>
    public static class Sound
    {
        /// <summary>
        /// The current provider, by default a no op that does not play sound
        /// </summary>
        public static ISoundProvider Provider = new NoOpSoundProvider();

        /// <summary>
        /// Plays the sound associated with the given id immediately and once
        /// </summary>
        /// <param name="soundId">a sound id</param>
        public static Task<Lifetime> Play(string soundId, float volume = 1) => Provider.Play(soundId.ToLower(), volume);

        /// <summary>
        /// Plays the sound associated with the given id immidiately and in a loop
        /// </summary>
        /// <param name="soundId">a sound id</param>
        /// <returns>a promist to a disposable that can be used to stop the loop</returns>
        public static Task<IDisposable> Loop(string soundId, float volume = .1f) => Provider.Loop(soundId.ToLower(), volume);
        
        /// <summary>
        /// Disposes the current provider and resets the provider to a no op provider
        /// </summary>
        public static void Dispose()
        {
            Provider.Dispose();
            Provider = new NoOpSoundProvider();
        }
    }

    /// <summary>
    /// The interface for playing sound in PowerArgs games
    /// </summary>
    public interface ISoundProvider : IDisposable
    {
        bool IsReady { get; }
        /// <summary>
        /// Plays the sound associated with the given id immediately and once
        /// </summary>
        /// <param name="soundId">a sound id</param>
        Task<Lifetime> Play(string soundId, float volume);

        /// <summary>
        /// Plays the sound associated with the given id immidiately and in a loop
        /// </summary>
        /// <param name="soundId">a sound id</param>
        /// <returns>a promist to a disposable that can be used to stop the loop</returns>
        Task<IDisposable> Loop(string soundId, float volume);
    }

    /// <summary>
    /// A sound provider that does not play sound
    /// </summary>
    public class NoOpSoundProvider : DummyDisposable, ISoundProvider
    {
        public bool IsReady => true;

        /// <summary>
        /// Does nothing
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name="soundId">unused</param>
        public Task<Lifetime> Play(string soundId, float volume)
        {
            var d = new TaskCompletionSource<Lifetime>();
            var l = new Lifetime();
            l.Dispose();
            d.SetResult(l);
            return d.Task;
        }

        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name="soundId">unused</param>
        /// <returns>a Task that resolves immediately to a dummy disposable</returns>
        public Task<IDisposable> Loop(string soundId, float volume)
        {
            var d = new TaskCompletionSource<IDisposable>();
            d.SetResult(new DummyDisposable());
            return d.Task;
        }
    }
}
