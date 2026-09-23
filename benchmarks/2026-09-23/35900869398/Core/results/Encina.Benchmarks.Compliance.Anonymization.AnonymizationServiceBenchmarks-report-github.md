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
| &#39;Tokenize: UUID format&#39;                   |  13.968 μs |  3.6007 μs | 0.1974 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4217 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.945 μs |  4.5766 μs | 0.2509 μs |  1.00 |    0.02 |    5 |  0.2136 | 0.0763 |    3688 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.373 μs |  1.0259 μs | 0.0562 μs |  0.31 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.259 μs |  0.1116 μs | 0.0061 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.612 μs |  0.0669 μs | 0.0037 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.688 μs |  0.1339 μs | 0.0073 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 344.128 μs | 38.3123 μs | 2.1000 μs | 24.64 |    0.33 |    6 | 17.0898 | 0.4883 |  291296 B |       69.08 |
