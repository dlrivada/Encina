```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.813 μs |   2.4755 μs | 0.1357 μs |  1.00 |    0.02 |    4 | 0.0458 | 0.0305 |    4088 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.264 μs |   5.8389 μs | 0.3200 μs |  1.04 |    0.03 |    4 | 0.0458 | 0.0305 |    4170 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.326 μs |   0.5504 μs | 0.0302 μs |  0.31 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.870 μs |   0.3300 μs | 0.0181 μs |  0.17 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.081 μs |   0.9191 μs | 0.0504 μs |  0.56 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.965 μs |   0.0926 μs | 0.0051 μs |  0.18 |    0.00 |    1 | 0.0076 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 329.318 μs | 116.1959 μs | 6.3691 μs | 30.46 |    0.61 |    5 | 3.4180 |      - |  291296 B |       71.26 |
