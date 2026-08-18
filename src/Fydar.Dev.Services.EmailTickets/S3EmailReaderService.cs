using Amazon.S3;
using Amazon.S3.Model;
using Fydar.Dev.Services.EmailTickets.Models;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fydar.Dev.Services.EmailTickets;

public class S3EmailReaderService : IEmailReaderService
{
    // A deleted ticket is moved under this prefix rather than erased, so it can be restored.
    private const string TrashPrefix = "deleted/";

    private readonly IAmazonS3 amazonS3;
    private readonly S3EmailReaderServiceConfiguration configuration;

    public S3EmailReaderService(
        IAmazonS3 amazonS3,
        S3EmailReaderServiceConfiguration configuration)
    {
        this.amazonS3 = amazonS3;
        this.configuration = configuration;
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
                    if (s3Object.Key.EndsWith("/") || s3Object.Key.StartsWith(TrashPrefix, StringComparison.Ordinal))
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
    public async Task DeleteEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        await MoveAsync(ticketId, TrashPrefix + ticketId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RestoreEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        await MoveAsync(TrashPrefix + ticketId, ticketId, cancellationToken);
    }

    private async Task MoveAsync(
        string sourceKey,
        string destinationKey,
        CancellationToken cancellationToken)
    {
        await amazonS3.CopyObjectAsync(new CopyObjectRequest()
        {
            SourceBucket = configuration.Bucket,
            SourceKey = sourceKey,
            DestinationBucket = configuration.Bucket,
            DestinationKey = destinationKey
        }, cancellationToken);

        await amazonS3.DeleteObjectAsync(new DeleteObjectRequest()
        {
            BucketName = configuration.Bucket,
            Key = sourceKey
        }, cancellationToken);
    }
}
