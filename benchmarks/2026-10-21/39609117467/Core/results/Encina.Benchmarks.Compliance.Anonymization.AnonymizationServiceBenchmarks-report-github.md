```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.39GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  12.636 μs | 10.0153 μs | 0.5490 μs |  1.00 |    0.05 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.864 μs |  8.3392 μs | 0.4571 μs |  1.02 |    0.05 |    5 |  0.2136 | 0.0763 |    3688 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.830 μs |  0.7613 μs | 0.0417 μs |  0.30 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.789 μs |  0.0224 μs | 0.0012 μs |  0.14 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.679 μs |  0.1691 μs | 0.0093 μs |  0.53 |    0.02 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.233 μs |  0.3159 μs | 0.0173 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 256.753 μs | 60.7409 μs | 3.3294 μs | 20.34 |    0.82 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
