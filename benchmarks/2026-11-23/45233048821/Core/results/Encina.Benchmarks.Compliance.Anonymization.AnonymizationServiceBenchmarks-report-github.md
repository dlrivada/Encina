```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   7.567 μs | 1.1577 μs | 0.0635 μs |  1.00 |    0.01 |    4 | 0.0458 | 0.0305 |    4080 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   7.840 μs | 4.4593 μs | 0.2444 μs |  1.04 |    0.03 |    4 | 0.0305 | 0.0153 |    3568 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.238 μs | 0.2294 μs | 0.0126 μs |  0.30 |    0.00 |    2 | 0.0038 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.345 μs | 0.1082 μs | 0.0059 μs |  0.18 |    0.00 |    1 | 0.0038 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3.902 μs | 0.5638 μs | 0.0309 μs |  0.52 |    0.01 |    3 | 0.0076 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.477 μs | 0.0495 μs | 0.0027 μs |  0.20 |    0.00 |    1 | 0.0095 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 244.366 μs | 1.5072 μs | 0.0826 μs | 32.30 |    0.23 |    5 | 3.4180 |      - |  291296 B |       71.40 |
