```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.142 μs |  3.8273 μs | 0.2098 μs |  1.00 |    0.02 |    5 |  0.1526 | 0.0763 |    4210 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.994 μs |  4.9494 μs | 0.2713 μs |  0.99 |    0.02 |    5 |  0.1526 | 0.0763 |    4286 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.517 μs |  0.3257 μs | 0.0179 μs |  0.32 |    0.00 |    3 |  0.0229 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.560 μs |  0.1106 μs | 0.0061 μs |  0.18 |    0.00 |    2 |  0.0153 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.953 μs |  0.6143 μs | 0.0337 μs |  0.56 |    0.01 |    4 |  0.0305 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.536 μs |  0.0493 μs | 0.0027 μs |  0.11 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 354.238 μs | 15.4510 μs | 0.8469 μs | 25.05 |    0.32 |    6 | 11.2305 |      - |  291296 B |       69.19 |
