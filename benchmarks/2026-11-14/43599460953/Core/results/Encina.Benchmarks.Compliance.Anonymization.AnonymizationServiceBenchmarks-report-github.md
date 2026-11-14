```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.197 μs |  3.0258 μs | 0.1659 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4096 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.051 μs |  1.0951 μs | 0.0600 μs |  0.99 |    0.01 |    5 |  0.1984 | 0.0610 |    3568 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.672 μs |  0.1758 μs | 0.0096 μs |  0.28 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.080 μs |  0.0913 μs | 0.0050 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.629 μs |  0.3986 μs | 0.0218 μs |  0.50 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.703 μs |  0.1438 μs | 0.0079 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 327.752 μs | 23.9615 μs | 1.3134 μs | 24.84 |    0.28 |    6 | 17.0898 | 0.4883 |  291296 B |       71.12 |
