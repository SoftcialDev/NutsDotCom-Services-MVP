using ComplaintAnalysis.Domain.Entities;
using ComplaintAnalysis.Domain.Models;

namespace ComplaintAnalysis.Domain.Services;

public interface IComplaintAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(Complaint complaint);
}

