```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  12.976 μs |  3.7232 μs | 0.2041 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4094 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.584 μs |  5.7642 μs | 0.3160 μs |  1.05 |    0.03 |    5 |  0.1984 | 0.0610 |    3568 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.675 μs |  0.0990 μs | 0.0054 μs |  0.28 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.160 μs |  0.1008 μs | 0.0055 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.650 μs |  0.6502 μs | 0.0356 μs |  0.51 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.728 μs |  0.1422 μs | 0.0078 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 338.224 μs | 31.9066 μs | 1.7489 μs | 26.07 |    0.38 |    6 | 17.0898 | 0.4883 |  291296 B |       71.15 |
