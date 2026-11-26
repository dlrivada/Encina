```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8.287 μs | 3.6850 μs | 0.2020 μs |  1.00 |    0.03 |    4 | 0.0305 | 0.0153 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8.175 μs | 4.4746 μs | 0.2453 μs |  0.99 |    0.03 |    4 | 0.0458 | 0.0305 |    4157 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.124 μs | 0.2462 μs | 0.0135 μs |  0.26 |    0.01 |    2 | 0.0038 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.373 μs | 0.1137 μs | 0.0062 μs |  0.17 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3.997 μs | 0.8779 μs | 0.0481 μs |  0.48 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.500 μs | 0.0926 μs | 0.0051 μs |  0.18 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 248.211 μs | 8.3403 μs | 0.4572 μs | 29.96 |    0.64 |    5 | 3.4180 |      - |  291296 B |       83.32 |
