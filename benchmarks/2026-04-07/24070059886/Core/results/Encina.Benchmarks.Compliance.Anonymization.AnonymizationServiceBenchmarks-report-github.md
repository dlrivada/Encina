```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                    | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|------:|------:|-----:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   | 53.99 ms |    NA |  1.00 |    7 |    3808 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               | 53.48 ms |    NA |  0.99 |    6 |    3896 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               | 16.59 ms |    NA |  0.31 |    2 |     856 B |        0.22 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               | 15.35 ms |    NA |  0.28 |    1 |     384 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; | 21.06 ms |    NA |  0.39 |    4 |    1312 B |        0.34 |
| &#39;Anonymize: data masking (2 fields)&#39;      | 18.60 ms |    NA |  0.34 |    3 |   11760 B |        3.09 |
| &#39;Risk assessment: 100-record dataset&#39;     | 26.81 ms |    NA |  0.50 |    5 |  291296 B |       76.50 |
