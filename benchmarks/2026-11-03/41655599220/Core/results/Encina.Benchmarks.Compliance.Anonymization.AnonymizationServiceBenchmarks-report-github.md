```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8.371 μs |  2.0675 μs | 0.1133 μs |  1.00 |    0.02 |    4 | 0.0458 | 0.0305 |    4095 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8.475 μs |  4.5062 μs | 0.2470 μs |  1.01 |    0.03 |    4 | 0.0305 | 0.0153 |    3568 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.201 μs |  0.3168 μs | 0.0174 μs |  0.26 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.367 μs |  0.2759 μs | 0.0151 μs |  0.16 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3.922 μs |  0.4681 μs | 0.0257 μs |  0.47 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.467 μs |  0.1351 μs | 0.0074 μs |  0.18 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 247.797 μs | 11.4139 μs | 0.6256 μs | 29.61 |    0.35 |    5 | 3.4180 |      - |  291296 B |       71.13 |
