```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   7.648 μs |   6.5604 μs | 0.3596 μs |  1.00 |    0.06 |    4 | 0.0458 | 0.0305 |    4093 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   7.961 μs |   1.9937 μs | 0.1093 μs |  1.04 |    0.05 |    4 | 0.0381 | 0.0305 |    3584 B |        0.88 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   1.947 μs |   0.2789 μs | 0.0153 μs |  0.25 |    0.01 |    2 | 0.0038 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.286 μs |   0.7104 μs | 0.0389 μs |  0.17 |    0.01 |    1 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3.969 μs |   1.5443 μs | 0.0846 μs |  0.52 |    0.02 |    3 | 0.0114 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.382 μs |   0.1451 μs | 0.0080 μs |  0.18 |    0.01 |    1 | 0.0095 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 226.882 μs | 101.5098 μs | 5.5641 μs | 29.71 |    1.39 |    5 | 3.4180 |      - |  291296 B |       71.17 |
