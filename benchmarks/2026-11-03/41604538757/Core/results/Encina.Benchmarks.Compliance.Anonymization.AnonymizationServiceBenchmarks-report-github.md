```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.200 μs |  1.0783 μs | 0.0591 μs |  1.00 |    0.01 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.993 μs |  3.6051 μs | 0.1976 μs |  0.98 |    0.01 |    5 |  0.2594 | 0.1068 |    4762 B |        1.36 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.704 μs |  0.0467 μs | 0.0026 μs |  0.28 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.122 μs |  0.1247 μs | 0.0068 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.125 μs |  0.1262 μs | 0.0069 μs |  0.54 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.732 μs |  0.1237 μs | 0.0068 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 345.516 μs | 15.4585 μs | 0.8473 μs | 26.17 |    0.12 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
