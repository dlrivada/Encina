```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.748 μs |  3.9006 μs | 0.2138 μs |  1.00 |    0.02 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.229 μs |  6.2091 μs | 0.3403 μs |  0.96 |    0.03 |    5 |  0.1984 | 0.0610 |    3568 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.843 μs |  0.4412 μs | 0.0242 μs |  0.24 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.526 μs |  0.3971 μs | 0.0218 μs |  0.13 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.367 μs |  0.6585 μs | 0.0361 μs |  0.46 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.103 μs |  0.1756 μs | 0.0096 μs |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 245.686 μs | 25.1580 μs | 1.3790 μs | 20.92 |    0.35 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
