using Fido2NetLib;

namespace SeniorAiChat.Api.Passkeys;

internal sealed class LocalMetadataService : IMetadataService
{
    public Task<MetadataBLOBPayloadEntry?> GetEntryAsync(
        Guid aaguid,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<MetadataBLOBPayloadEntry?>(null);
    }

    public bool ConformanceTesting()
    {
        return false;
    }
}
