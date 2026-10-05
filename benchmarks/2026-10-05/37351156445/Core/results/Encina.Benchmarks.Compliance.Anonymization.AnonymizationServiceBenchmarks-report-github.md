```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  17.030 μs | 11.6718 μs | 0.6398 μs |  1.00 |    0.05 |    5 |  0.2136 | 0.0916 |    4197 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  15.893 μs |  2.0142 μs | 0.1104 μs |  0.93 |    0.03 |    5 |  0.2441 | 0.1221 |    4809 B |        1.15 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   5.077 μs |  0.4428 μs | 0.0243 μs |  0.30 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.424 μs |  0.0575 μs | 0.0032 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.814 μs |  2.3107 μs | 0.1267 μs |  0.52 |    0.02 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.738 μs |  0.1315 μs | 0.0072 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 369.120 μs | 46.3010 μs | 2.5379 μs | 21.69 |    0.71 |    6 | 17.0898 | 0.4883 |  291296 B |       69.41 |
