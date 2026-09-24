```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.526 μs |  8.2278 μs | 0.4510 μs |  1.00 |    0.05 |    5 |  0.2289 | 0.0916 |    4216 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.086 μs |  2.4904 μs | 0.1365 μs |  0.96 |    0.03 |    5 |  0.2136 | 0.0763 |    3688 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.425 μs |  0.1410 μs | 0.0077 μs |  0.30 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.711 μs |  0.0403 μs | 0.0022 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.238 μs |  0.3904 μs | 0.0214 μs |  0.54 |    0.02 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.114 μs |  0.0813 μs | 0.0045 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 248.378 μs | 39.7924 μs | 2.1812 μs | 21.57 |    0.73 |    6 | 17.3340 | 0.4883 |  291296 B |       69.09 |
