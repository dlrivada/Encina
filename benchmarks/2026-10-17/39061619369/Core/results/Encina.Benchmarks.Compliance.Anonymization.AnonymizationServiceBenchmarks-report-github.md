```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.153 μs |  3.6082 μs | 0.1978 μs |  1.00 |    0.02 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.618 μs |  4.6360 μs | 0.2541 μs |  1.03 |    0.02 |    5 |  0.2441 | 0.0916 |    4289 B |        1.18 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.799 μs |  0.3447 μs | 0.0189 μs |  0.34 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.328 μs |  0.0928 μs | 0.0051 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.809 μs |  0.8831 μs | 0.0484 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.784 μs |  0.0327 μs | 0.0018 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 347.227 μs | 16.1214 μs | 0.8837 μs | 24.54 |    0.30 |    6 | 17.0898 | 0.4883 |  291296 B |       80.38 |
