namespace Log.Tests;

using FluentAssertions;
using global::Log.Api.Controllers.Public;
using global::Log.Domain.Exceptions;
using Xunit;

public sealed class LogBatchRetiredTests
{
    [Fact]
    public void Posting_a_batch_answers_410_with_a_clear_code_instead_of_accepting_and_dropping_it()
    {
        var act = () => new LogBatchController().BatchLog();

        var error = act.Should().Throw<DomainException>().Which;
        error.StatusCode.Should().Be(410);
        error.ErrorCode.Should().Be(LogBatchController.RetiredErrorCode);
        error.Message.Should().NotContain("accepted");
    }
}
