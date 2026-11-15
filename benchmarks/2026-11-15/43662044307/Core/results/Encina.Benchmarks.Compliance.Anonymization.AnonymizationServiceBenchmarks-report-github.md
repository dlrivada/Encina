```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8.260 μs | 0.3007 μs | 0.4313 μs |  1.00 |    0.07 |    5 | 0.0305 | 0.0153 |    3504 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8.427 μs | 0.2307 μs | 0.3308 μs |  1.02 |    0.06 |    5 | 0.0305 | 0.0153 |    3584 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.150 μs | 0.0115 μs | 0.0169 μs |  0.26 |    0.01 |    3 | 0.0038 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.353 μs | 0.0090 μs | 0.0129 μs |  0.16 |    0.01 |    1 | 0.0038 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3.976 μs | 0.0429 μs | 0.0641 μs |  0.48 |    0.03 |    4 | 0.0076 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.483 μs | 0.0075 μs | 0.0111 μs |  0.18 |    0.01 |    2 | 0.0095 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 242.845 μs | 1.4587 μs | 2.1834 μs | 29.48 |    1.50 |    6 | 3.4180 |      - |  291296 B |       83.13 |
