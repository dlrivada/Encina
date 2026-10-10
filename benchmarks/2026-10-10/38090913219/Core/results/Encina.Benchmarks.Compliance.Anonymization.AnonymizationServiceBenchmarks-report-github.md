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
| &#39;Tokenize: UUID format&#39;                   |  14.142 μs |  2.7302 μs | 0.1497 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4215 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.127 μs |  1.6043 μs | 0.0879 μs |  1.00 |    0.01 |    5 |  0.2441 | 0.1068 |    4281 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.786 μs |  0.2443 μs | 0.0134 μs |  0.34 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.306 μs |  0.1224 μs | 0.0067 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.717 μs |  0.3773 μs | 0.0207 μs |  0.55 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.827 μs |  0.0214 μs | 0.0012 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 342.560 μs | 20.3991 μs | 1.1181 μs | 24.22 |    0.23 |    6 | 17.0898 | 0.4883 |  291296 B |       69.11 |
