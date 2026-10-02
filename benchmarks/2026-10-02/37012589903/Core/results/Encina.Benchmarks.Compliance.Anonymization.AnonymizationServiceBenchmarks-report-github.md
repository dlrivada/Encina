```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  15.135 μs |  3.4124 μs | 0.1870 μs |  1.00 |    0.02 |    5 |  0.2441 | 0.1221 |    4748 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  15.763 μs | 25.6038 μs | 1.4034 μs |  1.04 |    0.08 |    5 |  0.2441 | 0.0916 |    4256 B |        0.90 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.439 μs |  0.2412 μs | 0.0132 μs |  0.29 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.326 μs |  0.1227 μs | 0.0067 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.759 μs |  0.6327 μs | 0.0347 μs |  0.51 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.785 μs |  0.0865 μs | 0.0047 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 355.071 μs | 77.1289 μs | 4.2277 μs | 23.46 |    0.35 |    6 | 17.0898 | 0.4883 |  291296 B |       61.35 |
