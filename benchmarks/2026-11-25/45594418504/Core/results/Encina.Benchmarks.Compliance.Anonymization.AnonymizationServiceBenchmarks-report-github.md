```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8.288 μs |  1.3432 μs | 0.0736 μs |  1.00 |    0.01 |    4 | 0.0458 | 0.0305 |    4666 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8.220 μs |  1.5906 μs | 0.0872 μs |  0.99 |    0.01 |    4 | 0.0305 | 0.0153 |    3568 B |        0.76 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.166 μs |  0.2165 μs | 0.0119 μs |  0.26 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.398 μs |  0.0753 μs | 0.0041 μs |  0.17 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3.992 μs |  0.5986 μs | 0.0328 μs |  0.48 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.542 μs |  0.0579 μs | 0.0032 μs |  0.19 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 249.320 μs | 11.0773 μs | 0.6072 μs | 30.08 |    0.24 |    5 | 3.4180 |      - |  291296 B |       62.43 |
