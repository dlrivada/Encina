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
| &#39;Tokenize: UUID format&#39;                   |  13.425 μs |  2.2394 μs | 0.1227 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4221 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.761 μs |  2.3958 μs | 0.1313 μs |  1.03 |    0.01 |    5 |  0.2136 | 0.0763 |    3688 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.307 μs |  0.1911 μs | 0.0105 μs |  0.32 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.269 μs |  0.0758 μs | 0.0042 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.162 μs |  0.5028 μs | 0.0276 μs |  0.61 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.802 μs |  0.0919 μs | 0.0050 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 346.782 μs | 30.3638 μs | 1.6643 μs | 25.83 |    0.23 |    6 | 17.0898 | 0.4883 |  291296 B |       69.01 |
