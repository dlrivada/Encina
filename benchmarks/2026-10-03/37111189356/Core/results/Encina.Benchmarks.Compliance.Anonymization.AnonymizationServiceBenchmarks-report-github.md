```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.219 μs |  1.7026 μs | 0.0933 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4221 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.202 μs |  4.0810 μs | 0.2237 μs |  1.00 |    0.01 |    5 |  0.2441 | 0.0916 |    4272 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.390 μs |  0.2409 μs | 0.0132 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.301 μs |  0.1050 μs | 0.0058 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   8.314 μs |  0.1800 μs | 0.0099 μs |  0.58 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.773 μs |  0.0822 μs | 0.0045 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 357.085 μs | 35.8545 μs | 1.9653 μs | 25.11 |    0.19 |    6 | 17.0898 | 0.4883 |  291296 B |       69.01 |
