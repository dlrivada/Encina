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
| &#39;Tokenize: UUID format&#39;                   |   8.446 μs |  1.4946 μs | 0.0819 μs |  1.00 |    0.01 |    4 | 0.0458 | 0.0305 |    4098 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8.289 μs |  2.6723 μs | 0.1465 μs |  0.98 |    0.02 |    4 | 0.0458 | 0.0305 |    4761 B |        1.16 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.225 μs |  0.2543 μs | 0.0139 μs |  0.26 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.360 μs |  0.1710 μs | 0.0094 μs |  0.16 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.001 μs |  0.0734 μs | 0.0040 μs |  0.47 |    0.00 |    3 | 0.0076 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.481 μs |  0.1338 μs | 0.0073 μs |  0.18 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 250.413 μs | 10.8359 μs | 0.5940 μs | 29.65 |    0.26 |    5 | 3.4180 |      - |  291296 B |       71.08 |
