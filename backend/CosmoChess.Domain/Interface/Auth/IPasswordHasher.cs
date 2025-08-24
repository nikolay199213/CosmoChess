﻿namespace CosmoChess.Domain.Interface.Auth
{
    public interface IPasswordHasher
    {
        string Generate(string password);
        bool Verify(string password, string hash);
    }
}
