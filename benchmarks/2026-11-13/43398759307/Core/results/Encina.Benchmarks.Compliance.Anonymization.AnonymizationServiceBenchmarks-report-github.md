```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  12.798 μs |  0.2978 μs | 0.0163 μs |  1.00 |    0.00 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.769 μs |  2.5397 μs | 0.1392 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4164 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.646 μs |  1.1898 μs | 0.0652 μs |  0.28 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.071 μs |  0.2882 μs | 0.0158 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.377 μs |  0.4609 μs | 0.0253 μs |  0.50 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.692 μs |  0.0180 μs | 0.0010 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 334.669 μs | 35.0482 μs | 1.9211 μs | 26.15 |    0.13 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
