```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.385 μs |  3.6879 μs | 0.2021 μs |  1.00 |    0.02 |    5 |  0.1373 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.128 μs |  2.9001 μs | 0.1590 μs |  0.98 |    0.02 |    5 |  0.1526 | 0.0916 |    4160 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.345 μs |  0.0330 μs | 0.0018 μs |  0.25 |    0.00 |    3 |  0.0191 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.058 μs |  0.2199 μs | 0.0121 μs |  0.15 |    0.00 |    2 |  0.0153 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.210 μs |  0.1148 μs | 0.0063 μs |  0.46 |    0.01 |    4 |  0.0381 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.448 μs |  0.0431 μs | 0.0024 μs |  0.11 |    0.00 |    1 |  0.0362 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 358.044 μs | 17.2653 μs | 0.9464 μs | 26.75 |    0.36 |    6 | 11.2305 |      - |  291296 B |       83.32 |
