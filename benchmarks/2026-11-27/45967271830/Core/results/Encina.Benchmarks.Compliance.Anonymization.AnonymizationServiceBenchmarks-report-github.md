```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9.377 μs |   0.4857 μs | 0.0266 μs |  1.00 |    0.00 |    4 | 0.0305 | 0.0153 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   9.332 μs |   5.4320 μs | 0.2977 μs |  1.00 |    0.03 |    4 | 0.0458 | 0.0305 |    4162 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.947 μs |   1.2992 μs | 0.0712 μs |  0.31 |    0.01 |    2 | 0.0038 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.718 μs |   0.6726 μs | 0.0369 μs |  0.18 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.552 μs |   0.6429 μs | 0.0352 μs |  0.59 |    0.00 |    3 | 0.0076 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.747 μs |   0.6862 μs | 0.0376 μs |  0.19 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 289.233 μs | 119.1477 μs | 6.5309 μs | 30.84 |    0.61 |    5 | 3.4180 |      - |  291296 B |       83.32 |
