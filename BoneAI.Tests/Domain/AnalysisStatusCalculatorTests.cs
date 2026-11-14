using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Domain.Services;
using Xunit;

namespace BoneAI.Tests.Domain;

public class AnalysisStatusCalculatorTests
{
    [Fact]
    public void Calculate_EmptyRuns_ReturnsPending()
    {
        var status = AnalysisStatusCalculator.Calculate(Enumerable.Empty<AnalysisRun>());
        Assert.Equal(AnalysisStatus.Pending, status);
    }

    [Fact]
    public void Calculate_AllPending_ReturnsPending()
    {
        var runs = new[]
        {
            new AnalysisRun { Status = AnalysisRunStatus.Pending },
            new AnalysisRun { Status = AnalysisRunStatus.Pending }
        };

        var status = AnalysisStatusCalculator.Calculate(runs);
        Assert.Equal(AnalysisStatus.Pending, status);
    }

    [Fact]
    public void Calculate_AtLeastOneProcessing_ReturnsProcessing()
    {
        var runs = new[]
        {
            new AnalysisRun { Status = AnalysisRunStatus.Processing },
            new AnalysisRun { Status = AnalysisRunStatus.Pending }
        };

        var status = AnalysisStatusCalculator.Calculate(runs);
        Assert.Equal(AnalysisStatus.Processing, status);
    }

    [Fact]
    public void Calculate_OneCompletedAndOnePending_ReturnsProcessing()
    {
        var runs = new[]
        {
            new AnalysisRun { Status = AnalysisRunStatus.Completed },
            new AnalysisRun { Status = AnalysisRunStatus.Pending }
        };

        var status = AnalysisStatusCalculator.Calculate(runs);
        Assert.Equal(AnalysisStatus.Processing, status);
    }

    [Fact]
    public void Calculate_AllCompleted_ReturnsCompleted()
    {
        var runs = new[]
        {
            new AnalysisRun { Status = AnalysisRunStatus.Completed },
            new AnalysisRun { Status = AnalysisRunStatus.Completed },
            new AnalysisRun { Status = AnalysisRunStatus.Completed }
        };

        var status = AnalysisStatusCalculator.Calculate(runs);
        Assert.Equal(AnalysisStatus.Completed, status);
    }

    [Fact]
    public void Calculate_AllFailed_ReturnsFailed()
    {
        var runs = new[]
        {
            new AnalysisRun { Status = AnalysisRunStatus.Failed },
            new AnalysisRun { Status = AnalysisRunStatus.Failed }
        };

        var status = AnalysisStatusCalculator.Calculate(runs);
        Assert.Equal(AnalysisStatus.Failed, status);
    }

    [Fact]
    public void Calculate_PartialSuccessAndFailure_ReturnsPartiallyCompleted()
    {
        var runs = new[]
        {
            new AnalysisRun { Status = AnalysisRunStatus.Completed },
            new AnalysisRun { Status = AnalysisRunStatus.Failed }
        };

        var status = AnalysisStatusCalculator.Calculate(runs);
        Assert.Equal(AnalysisStatus.PartiallyCompleted, status);
    }
}
