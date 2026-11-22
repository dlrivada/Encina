```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.607 μs | 0.2699 μs | 0.3695 μs |  1.00 |    0.03 |    5 |  0.1984 | 0.0610 |    3504 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.349 μs | 0.3319 μs | 0.4543 μs |  0.98 |    0.04 |    5 |  0.2136 | 0.0763 |    3584 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.625 μs | 0.0139 μs | 0.0200 μs |  0.25 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.029 μs | 0.0213 μs | 0.0318 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.508 μs | 0.0316 μs | 0.0443 μs |  0.45 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.685 μs | 0.0115 μs | 0.0169 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 332.388 μs | 3.6749 μs | 5.2704 μs | 22.77 |    0.65 |    6 | 17.0898 | 0.4883 |  291296 B |       83.13 |
