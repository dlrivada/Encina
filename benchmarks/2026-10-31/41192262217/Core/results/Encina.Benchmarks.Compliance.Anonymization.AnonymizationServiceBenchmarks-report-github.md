```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.775 μs |  3.3119 μs | 0.1815 μs |  1.00 |    0.02 |    5 |  0.2594 | 0.1221 |    4683 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.036 μs |  3.1264 μs | 0.1714 μs |  1.02 |    0.02 |    5 |  0.2289 | 0.0916 |    4168 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.730 μs |  0.4661 μs | 0.0255 μs |  0.34 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.326 μs |  0.1659 μs | 0.0091 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.672 μs |  0.5637 μs | 0.0309 μs |  0.56 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.727 μs |  0.2343 μs | 0.0128 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 341.461 μs | 26.1574 μs | 1.4338 μs | 24.79 |    0.30 |    6 | 17.0898 | 0.4883 |  291296 B |       62.20 |
