using System.Text.RegularExpressions;
using ComplaintAnalysis.Domain.Models;
using ComplaintAnalysis.Domain.ValueObjects;

namespace ComplaintAnalysis.Domain.Services;

public class TextClassifier : ITextClassifier
{
    public ClassificationResult Classify(
        ProcessedText processedText,
        string sentiment,
        List<string> keyPhrases,
        List<string> entities,
        LabelConfiguration configuration)
    {
        var sentimentLower = sentiment.ToLower();
        var keyPhrasesLower = keyPhrases.Select(kp => kp.ToLower()).ToList();
        var text = processedText.NormalizedText;
        var tokens = processedText.Tokens;
        var negPositions = processedText.NegationSpans;
        var negWindow = configuration.Global.NegationWindowTokens;

        var labelsCfg = configuration.Labels.ToDictionary(l => l.Id, l => l);
        var results = new List<Label>();

        foreach (var label in configuration.Labels)
        {
            var (score, evidence) = ScoreLabel(
                label,
                text,
                tokens,
                negPositions,
                negWindow,
                keyPhrasesLower
            );

            var threshold = label.Threshold;
            if (score >= threshold)
            {
                var confidence = ComputeConfidence(score, threshold);
                results.Add(Label.Create(
                    label.Id,
                    label.DisplayName,
                    score,
                    confidence,
                    label.Priority,
                    evidence.Take(8).ToList() // Cap evidence
                ));
            }
        }

        // Sort by priority then score
        results = results.OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.Score)
            .ToList();

        // Resolve conflicts (exclusions/dominance)
        results = ApplyConflicts(results, labelsCfg);

        // Cap to top-N
        results = results.Take(configuration.Global.MaxLabelsPerComplaint).ToList();

        // Filter by confidence floor
        var minConf = configuration.Global.MinConfidenceToEmit;
        results = results.Where(r => r.Confidence >= minConf).ToList();

        var severity = ComputeSeverity(
            configuration.Global,
            sentimentLower,
            text,
            results.Select(r => r.Id).ToList()
        );

        return ClassificationResult.Create(sentimentLower, severity, results);
    }

    private (int score, List<MatchEvidence> evidence) ScoreLabel(
        LabelRule labelCfg,
        string text,
        List<string> tokens,
        List<int> negPositions,
        int negWindow,
        List<string> nlpKeyphrases)
    {
        var score = 0;
        var evidence = new List<MatchEvidence>();

        // 1) Keywords (token matches)
        var kwMap = labelCfg.Keywords.ToDictionary(
            k => k.T.ToLower(),
            k => k.W
        );

        for (int i = 0; i < tokens.Count; i++)
        {
            var tok = tokens[i];
            if (kwMap.TryGetValue(tok, out var weight))
            {
                if (IsNegated(i, negPositions, negWindow))
                {
                    // Penalize negated matches
                    score -= weight;
                    evidence.Add(MatchEvidence.Create("keyword", tok, -weight, $"... not {tok} ..."));
                }
                else
                {
                    score += weight;
                    evidence.Add(MatchEvidence.Create("keyword", tok, weight, $"... {tok} ..."));
                }
            }
        }

        // 2) Phrases (substring)
        foreach (var phrase in labelCfg.Phrases)
        {
            var phraseLower = phrase.T.ToLower();
            var weight = phrase.W;
            if (text.Contains(phraseLower))
            {
                score += weight;
                evidence.Add(MatchEvidence.Create("phrase", phraseLower, weight, phraseLower));
            }
        }

        // 3) Regex (pattern)
        foreach (var regexRule in labelCfg.Regex)
        {
            var pattern = regexRule.T;
            var weight = regexRule.W;
            var matches = RegexFinditerSafe(pattern, text);
            foreach (var match in matches)
            {
                var start = Math.Max(0, match.Index - 30);
                var length = Math.Min(text.Length - start, match.Length + 60);
                var excerpt = text.Substring(start, length);
                score += weight;
                evidence.Add(MatchEvidence.Create("regex", pattern, weight, excerpt));
            }
        }

        // 4) Keyphrases from NLP (optional boost)
        foreach (var kp in nlpKeyphrases)
        {
            foreach (var phrase in labelCfg.Phrases)
            {
                if (kp == phrase.T.ToLower())
                {
                    var weight = 1;
                    score += weight;
                    evidence.Add(MatchEvidence.Create("keyphrase", kp, weight, kp));
                }
            }
        }

        // 5) Hard rules (deterministic)
        foreach (var hr in labelCfg.HardRules)
        {
            if (hr.Type == "all_of_phrases")
            {
                var parts = hr.Phrases.Select(x => x.ToLower()).ToList();
                if (parts.All(part => text.Contains(part)))
                {
                    var weight = hr.EmitScore;
                    score += weight;
                    evidence.Add(MatchEvidence.Create(
                        "hard_rule",
                        string.Join(" + ", parts),
                        weight,
                        string.Join(" / ", parts)
                    ));
                }
            }
        }

        return (score, evidence);
    }

    private bool IsNegated(int matchTokenIndex, List<int> negPositions, int window)
    {
        // If a negation occurs within N tokens before the match token, treat as negated
        foreach (var npos in negPositions)
        {
            if (matchTokenIndex >= npos && matchTokenIndex - npos <= window)
            {
                return true;
            }
        }
        return false;
    }

    private List<Match> RegexFinditerSafe(string pattern, string text)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.Matches(text).Cast<Match>().ToList();
        }
        catch (ArgumentException)
        {
            return new List<Match>(); // fail-safe for invalid regex patterns
        }
    }

    private double ComputeConfidence(int score, int threshold)
    {
        // Simple, monotonic confidence mapping
        if (threshold <= 0)
        {
            return 1.0;
        }
        // 0 at (threshold-1), ~0.5 at threshold, approaches 1 as score grows
        return Math.Max(0.0, Math.Min(1.0, (score - (threshold - 1)) / (double)(threshold * 2)));
    }

    private List<Label> ApplyConflicts(
        List<Label> selected,
        Dictionary<string, LabelRule> labelsCfg)
    {
        var ids = selected.Select(x => x.Id).ToHashSet();
        var toRemove = new HashSet<string>();

        // Exclusions: if a label excludes others, remove them
        foreach (var x in selected)
        {
            var labelCfg = labelsCfg[x.Id];
            foreach (var ex in labelCfg.Exclusions)
            {
                if (ids.Contains(ex))
                {
                    toRemove.Add(ex);
                }
            }
        }

        // Dominance: if label dominates another, remove dominated if both present
        foreach (var x in selected)
        {
            var labelCfg = labelsCfg[x.Id];
            foreach (var dom in labelCfg.Dominates)
            {
                if (ids.Contains(dom))
                {
                    toRemove.Add(dom);
                }
            }
        }

        return selected.Where(x => !toRemove.Contains(x.Id)).ToList();
    }

    private string ComputeSeverity(
        GlobalConfiguration globalCfg,
        string sentiment,
        string text,
        List<string> labelIds)
    {
        var sevCfg = globalCfg.Severity;
        var txt = text.ToLower();

        // High: very_negative OR critical terms present
        if (sevCfg.High.Sentiment.Contains(sentiment))
        {
            return "HIGH";
        }
        if (sevCfg.High.CriticalTerms.Any(term => txt.Contains(term)))
        {
            return "HIGH";
        }

        // Medium: negative + critical labels
        if (sevCfg.Medium.Sentiment.Contains(sentiment))
        {
            var crit = sevCfg.Medium.CriticalLabels.ToHashSet();
            if (labelIds.Any(l => crit.Contains(l)))
            {
                return "MEDIUM";
            }
            return "MEDIUM"; // common in complaint contexts
        }

        return "LOW";
    }
}

