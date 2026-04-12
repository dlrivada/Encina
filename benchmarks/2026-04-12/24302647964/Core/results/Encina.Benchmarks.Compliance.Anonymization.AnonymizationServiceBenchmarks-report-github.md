```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.450 μs | 0.3415 μs | 0.4897 μs |  14.265 μs |  1.00 |    0.05 |    5 |  0.2136 | 0.0763 |    3632 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.485 μs | 0.2769 μs | 0.3697 μs |  14.546 μs |  1.00 |    0.04 |    5 |  0.2136 | 0.0763 |    3712 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.383 μs | 0.0152 μs | 0.0223 μs |   4.391 μs |  0.30 |    0.01 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.358 μs | 0.0354 μs | 0.0519 μs |   2.316 μs |  0.16 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.645 μs | 0.0300 μs | 0.0439 μs |   7.634 μs |  0.53 |    0.02 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.711 μs | 0.0213 μs | 0.0312 μs |   1.686 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 335.108 μs | 1.3193 μs | 1.9338 μs | 335.269 μs | 23.22 |    0.77 |    6 | 17.0898 | 0.4883 |  291296 B |       80.20 |
