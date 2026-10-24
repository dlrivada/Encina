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
| &#39;Tokenize: UUID format&#39;                   |  14.356 μs |  3.6143 μs | 0.1981 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4223 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.151 μs |  3.2349 μs | 0.1773 μs |  0.99 |    0.02 |    5 |  0.2441 | 0.1068 |    4287 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.839 μs |  0.2976 μs | 0.0163 μs |  0.34 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.418 μs |  0.0801 μs | 0.0044 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.505 μs |  0.5345 μs | 0.0293 μs |  0.59 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.772 μs |  0.0191 μs | 0.0010 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 339.031 μs | 22.2326 μs | 1.2186 μs | 23.62 |    0.29 |    6 | 17.0898 | 0.4883 |  291296 B |       68.98 |
