```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.097 μs |  3.4110 μs | 0.1870 μs |  1.00 |    0.02 |    4 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.075 μs |  3.3370 μs | 0.1829 μs |  1.00 |    0.02 |    4 |  0.1984 | 0.0610 |    3568 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.643 μs |  0.1166 μs | 0.0064 μs |  0.26 |    0.00 |    2 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.084 μs |  0.0829 μs | 0.0045 μs |  0.15 |    0.00 |    1 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.527 μs |  0.3266 μs | 0.0179 μs |  0.46 |    0.01 |    3 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.740 μs |  0.1421 μs | 0.0078 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 345.147 μs | 24.6390 μs | 1.3505 μs | 24.49 |    0.29 |    5 | 17.0898 | 0.4883 |  291296 B |       83.32 |
