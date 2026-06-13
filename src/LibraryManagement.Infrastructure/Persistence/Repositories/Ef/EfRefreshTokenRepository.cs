using LibraryManagement.Application.Auth;
using LibraryManagement.Application.Auth.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ef;

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly LibraryDbContext _db;

    public EfRefreshTokenRepository(LibraryDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<long> AddAsync(RefreshTokenModel token, CancellationToken cancellationToken = default)
    {
        var dataModel = new RefreshTokenDataModel
        {
            UserId = token.UserId,
            TokenHash = token.TokenHash,
            ExpiresAt = token.ExpiresAt,
            CreatedAt = token.CreatedAt,
            RevokedAt = token.RevokedAt
        };

        await _db.RefreshTokens.AddAsync(dataModel, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return dataModel.Id;
    }

    public async Task<RefreshTokenModel?> FindActiveByHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var dataModel = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash
                  && t.RevokedAt == null
                  && t.ExpiresAt > now,
                cancellationToken);

        return dataModel is null ? null : ToModel(dataModel);
    }

    public async Task RevokeAsync(long tokenId, CancellationToken cancellationToken = default)
    {
        var dataModel = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);

        if (dataModel is null || dataModel.RevokedAt is not null)
        {
            return;
        }

        dataModel.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var t in activeTokens)
        {
            t.RevokedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static RefreshTokenModel ToModel(RefreshTokenDataModel dataModel)
    {
        return new RefreshTokenModel(
            Id: dataModel.Id,
            UserId: dataModel.UserId,
            TokenHash: dataModel.TokenHash,
            ExpiresAt: dataModel.ExpiresAt,
            CreatedAt: dataModel.CreatedAt,
            RevokedAt: dataModel.RevokedAt);
    }
}