```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.378 μs |  3.4370 μs | 0.1884 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.1068 |    4214 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.264 μs |  2.3080 μs | 0.1265 μs |  0.99 |    0.01 |    5 |  0.2441 | 0.1221 |    4798 B |        1.14 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.391 μs |  0.3081 μs | 0.0169 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.435 μs |  0.0394 μs | 0.0022 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.756 μs |  0.5331 μs | 0.0292 μs |  0.54 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.694 μs |  0.0411 μs | 0.0023 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 357.372 μs | 28.7209 μs | 1.5743 μs | 24.86 |    0.30 |    6 | 17.0898 | 0.4883 |  291296 B |       69.13 |
