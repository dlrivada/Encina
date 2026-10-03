```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8.873 μs | 8.7093 μs | 0.4774 μs |  1.00 |    0.07 |    5 | 0.0458 | 0.0305 |    4223 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   9.084 μs | 2.4199 μs | 0.1326 μs |  1.03 |    0.05 |    5 | 0.0458 | 0.0305 |    4288 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.006 μs | 2.0501 μs | 0.1124 μs |  0.34 |    0.02 |    3 | 0.0038 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.758 μs | 0.1395 μs | 0.0076 μs |  0.20 |    0.01 |    2 | 0.0038 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.251 μs | 6.1445 μs | 0.3368 μs |  0.59 |    0.04 |    4 | 0.0076 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.356 μs | 0.0888 μs | 0.0049 μs |  0.15 |    0.01 |    1 | 0.0095 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 234.755 μs | 8.7105 μs | 0.4775 μs | 26.51 |    1.27 |    6 | 3.4180 |      - |  291296 B |       68.98 |
