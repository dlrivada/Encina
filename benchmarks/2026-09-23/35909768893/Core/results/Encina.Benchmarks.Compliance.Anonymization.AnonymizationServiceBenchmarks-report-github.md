```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.141 μs |  1.8735 μs | 0.1027 μs |  1.00 |    0.01 |    5 |  0.1526 | 0.0763 |    4222 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.424 μs |  2.7449 μs | 0.1505 μs |  1.02 |    0.01 |    5 |  0.1526 | 0.0763 |    4289 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.442 μs |  0.0691 μs | 0.0038 μs |  0.31 |    0.00 |    3 |  0.0229 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.516 μs |  0.1023 μs | 0.0056 μs |  0.18 |    0.00 |    2 |  0.0153 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.924 μs |  0.7179 μs | 0.0394 μs |  0.56 |    0.00 |    4 |  0.0305 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.430 μs |  0.0455 μs | 0.0025 μs |  0.10 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 358.918 μs | 11.3963 μs | 0.6247 μs | 25.38 |    0.16 |    6 | 11.2305 |      - |  291296 B |       68.99 |
