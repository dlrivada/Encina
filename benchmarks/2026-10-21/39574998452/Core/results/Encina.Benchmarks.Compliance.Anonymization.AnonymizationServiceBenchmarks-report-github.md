```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9.658 μs |  8.2081 μs | 0.4499 μs |  1.00 |    0.06 |    5 | 0.0458 | 0.0305 |    4225 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.024 μs | 11.3251 μs | 0.6208 μs |  1.04 |    0.07 |    5 | 0.0458 | 0.0305 |    4289 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.012 μs |  4.1013 μs | 0.2248 μs |  0.31 |    0.02 |    3 | 0.0038 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.743 μs |  0.7314 μs | 0.0401 μs |  0.18 |    0.01 |    2 | 0.0038 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.203 μs |  2.4182 μs | 0.1325 μs |  0.54 |    0.02 |    4 | 0.0076 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.373 μs |  0.0270 μs | 0.0015 μs |  0.14 |    0.01 |    1 | 0.0095 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 256.443 μs | 27.2082 μs | 1.4914 μs | 26.59 |    1.06 |    6 | 3.4180 |      - |  291296 B |       68.95 |
