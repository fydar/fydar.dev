using Amazon.S3;
using Amazon.S3.Model;
using Fydar.Dev.Services.EmailTickets.Models;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fydar.Dev.Services.EmailTickets;

public class S3EmailReaderService : IEmailReaderService
{
    // A deleted ticket is moved under this prefix rather than erased, so it can be restored.
    private const string trashPrefix = "deleted/";

    private readonly IAmazonS3 amazonS3;
    private readonly S3EmailReaderServiceConfiguration configuration;

    public S3EmailReaderService(
        IAmazonS3 amazonS3,
        IOptions<S3EmailReaderServiceConfiguration> configuration)
    {
        this.amazonS3 = amazonS3;
        this.configuration = configuration.Value;
    }

    public async Task<MimeMessage> ReadEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest()
        {
            BucketName = configuration.Bucket,
            Key = ticketId
        };
        using var response = await amazonS3.GetObjectAsync(request, cancellationToken);

        return await MimeMessage.LoadAsync(response.ResponseStream, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TicketPageModel> ListEmailsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageSize < 1)
        {
            pageSize = 1;
        }

        // The bucket keys tickets by message id, so they carry no ordering of their own and the
        // service can't page in S3's own order. Newest first means reading the whole listing and
        // sorting it here.
        var summaries = new List<TicketSummaryModel>();

        string? continuationToken = null;
        do
        {
            var request = new ListObjectsV2Request()
            {
                BucketName = configuration.Bucket,
                ContinuationToken = continuationToken
            };
            var response = await amazonS3.ListObjectsV2Async(request, cancellationToken);

            if (response.S3Objects != null)
            {
                foreach (var s3Object in response.S3Objects)
                {
                    // A key ending in a slash is a folder marker rather than an email, and a key
                    // under the trash prefix is a deleted ticket, awaiting a possible restore.
                    if (s3Object.Key.EndsWith("/") || s3Object.Key.StartsWith(trashPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    DateTime? lastModified = s3Object.LastModified;
                    long? size = s3Object.Size;

                    summaries.Add(new TicketSummaryModel()
                    {
                        TicketId = s3Object.Key,
                        LastModified = lastModified.GetValueOrDefault(),
                        Size = size.GetValueOrDefault()
                    });
                }
            }

            continuationToken = response.IsTruncated == true
                ? response.NextContinuationToken
                : null;
        }
        while (!string.IsNullOrEmpty(continuationToken));

        // Two tickets written within the same second would otherwise swap places between requests,
        // which would let a ticket slip across a page boundary and never be seen.
        summaries.Sort((left, right) =>
        {
            int comparison = right.LastModified.CompareTo(left.LastModified);

            return comparison != 0
                ? comparison
                : string.CompareOrdinal(left.TicketId, right.TicketId);
        });

        int totalCount = summaries.Count;
        int totalPages = (totalCount + pageSize - 1) / pageSize;

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        if (pageNumber > totalPages)
        {
            pageNumber = totalPages < 1 ? 1 : totalPages;
        }

        int skip = (pageNumber - 1) * pageSize;
        int take = Math.Max(0, Math.Min(pageSize, totalCount - skip));

        return new TicketPageModel()
        {
            Tickets = summaries.GetRange(skip, take),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> DeleteEmailsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default)
    {
        return await MoveManyAsync(ticketIds, toTrash: true, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> RestoreEmailsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default)
    {
        return await MoveManyAsync(ticketIds, toTrash: false, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> MoveManyAsync(
        IReadOnlyCollection<string> ticketIds,
        bool toTrash,
        CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        var copyResults = await Task.WhenAll(ticketIds.Select(async ticketId =>
        {
            string sourceKey = toTrash ? ticketId : trashPrefix + ticketId;
            string destinationKey = toTrash ? trashPrefix + ticketId : ticketId;

            try
            {
                await amazonS3.CopyObjectAsync(new CopyObjectRequest()
                {
                    SourceBucket = configuration.Bucket,
                    SourceKey = sourceKey,
                    DestinationBucket = configuration.Bucket,
                    DestinationKey = destinationKey
                }, cancellationToken);

                return (ticketId, sourceKey, copied: true);
            }
            catch (AmazonS3Exception)
            {
                // Most likely this ticket was already moved by a previous request; leave it out
                // of the batch delete below rather than letting one bad id fail the whole batch.
                return (ticketId, sourceKey, copied: false);
            }
        }));

        var ticketIdBySourceKey = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var result in copyResults)
        {
            if (result.copied)
            {
                ticketIdBySourceKey[result.sourceKey] = result.ticketId;
            }
        }

        if (ticketIdBySourceKey.Count == 0)
        {
            return Array.Empty<string>();
        }

        var movedTicketIds = new List<string>();

        // S3 accepts at most 1000 keys per DeleteObjects call.
        foreach (var chunk in Chunk(ticketIdBySourceKey.Keys, 1000))
        {
            var request = new DeleteObjectsRequest()
            {
                BucketName = configuration.Bucket,
                Objects = chunk.Select(key => new KeyVersion() { Key = key }).ToList(),
                Quiet = false
            };

            List<DeletedObject> deletedObjects;
            try
            {
                var response = await amazonS3.DeleteObjectsAsync(request, cancellationToken);
                deletedObjects = response.DeletedObjects;
            }
            catch (DeleteObjectsException exception)
            {
                // Some keys in the batch failed; the exception still carries the response, which
                // lists which ones succeeded.
                deletedObjects = exception.Response.DeletedObjects;
            }

            foreach (var deletedObject in deletedObjects)
            {
                if (ticketIdBySourceKey.TryGetValue(deletedObject.Key, out string? ticketId))
                {
                    movedTicketIds.Add(ticketId);
                }
            }
        }

        return movedTicketIds;
    }

    private static IEnumerable<IReadOnlyList<T>> Chunk<T>(IEnumerable<T> source, int size)
    {
        var buffer = new List<T>(size);

        foreach (var item in source)
        {
            buffer.Add(item);

            if (buffer.Count == size)
            {
                yield return buffer;
                buffer = new List<T>(size);
            }
        }

        if (buffer.Count > 0)
        {
            yield return buffer;
        }
    }
}
