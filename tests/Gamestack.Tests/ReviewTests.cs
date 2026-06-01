using Gamestack.Core.Models;
using Gamestack.Core.Notifications;
using Gamestack.Core.Versioning;
using Gamestack.Tests.Fakes;

namespace Gamestack.Tests;

public class ReviewTests
{
    private static readonly UserIdentity Alice = new("alice", "Alice", "alice@studio.test");
    private static readonly UserIdentity Bob = new("bob", "Bob", "bob@studio.test");

    private static (ManifestService svc, Manifest m) WithPushedFile()
    {
        var svc = new ManifestService(new FixedClock(new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero)));
        var m = svc.CreateNew("Demo");
        svc.AddVersion(m, "hero.psd", "sha", 10, Alice, "v1");
        return (svc, m);
    }

    [Fact]
    public void RequestReview_assigns_pending_review_to_the_version()
    {
        var (svc, m) = WithPushedFile();

        var req = svc.RequestReview(m, "hero.psd", 1, Bob, Alice);

        Assert.Equal(ReviewStatus.Pending, req.Status);
        Assert.Equal(Bob, m.Files["hero.psd"].Versions[0].Review!.Reviewer);
        Assert.Equal(Alice, m.Files["hero.psd"].Versions[0].Review!.RequestedBy);
        Assert.Null(m.Files["hero.psd"].Versions[0].Review!.DecidedAtUtc);
    }

    [Fact]
    public void SubmitReviewDecision_approve_sets_status_and_adds_approve_comment()
    {
        var (svc, m) = WithPushedFile();
        svc.RequestReview(m, "hero.psd", 1, Bob, Alice);

        var comment = svc.SubmitReviewDecision(m, "hero.psd", 1, Bob, ReviewStatus.Approved, "looks great");

        var file = m.Files["hero.psd"];
        Assert.Equal(ReviewStatus.Approved, file.Versions[0].Review!.Status);
        Assert.NotNull(file.Versions[0].Review!.DecidedAtUtc);
        Assert.Equal(CommentKind.Approve, comment.Kind);
        Assert.Contains(file.Comments, c => c.Kind == CommentKind.Approve);
    }

    [Fact]
    public void SubmitReviewDecision_request_changes_sets_status()
    {
        var (svc, m) = WithPushedFile();
        svc.RequestReview(m, "hero.psd", 1, Bob, Alice);

        svc.SubmitReviewDecision(m, "hero.psd", 1, Bob, ReviewStatus.ChangesRequested, "fix the armor");

        Assert.Equal(ReviewStatus.ChangesRequested, m.Files["hero.psd"].Versions[0].Review!.Status);
    }

    [Fact]
    public void SubmitReviewDecision_rejects_a_non_verdict_status()
    {
        var (svc, m) = WithPushedFile();
        Assert.Throws<ArgumentException>(() =>
            svc.SubmitReviewDecision(m, "hero.psd", 1, Bob, ReviewStatus.Pending, "x"));
    }

    [Fact]
    public void RequestReview_on_missing_version_throws()
    {
        var (svc, m) = WithPushedFile();
        Assert.Throws<ArgumentException>(() => svc.RequestReview(m, "hero.psd", 99, Bob, Alice));
    }

    [Fact]
    public void Notification_messages_target_the_right_recipient()
    {
        var requested = ReviewNotifications.Requested("hero.psd", 1, Bob, Alice, "please review");
        Assert.Equal(Bob.Email, requested.RecipientEmail);
        Assert.Contains("Review requested", requested.Subject);

        var decided = ReviewNotifications.Decided("hero.psd", 1, ReviewStatus.Approved, Bob, Alice, "ok");
        Assert.Equal(Alice.Email, decided.RecipientEmail); // requester is notified of the verdict
        Assert.Contains("approved", decided.Subject);
    }
}
