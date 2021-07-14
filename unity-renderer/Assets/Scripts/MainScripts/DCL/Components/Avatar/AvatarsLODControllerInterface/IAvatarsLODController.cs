using System;

namespace DCL
{
    public interface IAvatarsLODController : IDisposable
    {
        void Update();
        void RegisterAvatar(IAvatarRenderer newAvatar);
        void RemoveAvatar(IAvatarRenderer targetAvatar);
    }
}