```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.915 μs |  1.3723 μs | 0.0752 μs |  1.00 |    0.01 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.888 μs |  1.1337 μs | 0.0621 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0763 |    4170 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.650 μs |  0.5062 μs | 0.0277 μs |  0.33 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.341 μs |  0.0676 μs | 0.0037 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.605 μs |  0.2980 μs | 0.0163 μs |  0.55 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.706 μs |  0.1245 μs | 0.0068 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 330.136 μs | 39.5145 μs | 2.1659 μs | 23.73 |    0.17 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
