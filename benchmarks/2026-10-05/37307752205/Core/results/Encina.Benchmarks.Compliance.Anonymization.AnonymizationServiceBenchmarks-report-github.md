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
| &#39;Tokenize: UUID format&#39;                   |  14.561 μs |  4.4583 μs | 0.2444 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4221 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.164 μs |  0.9904 μs | 0.0543 μs |  0.97 |    0.01 |    5 |  0.2594 | 0.1068 |    4884 B |        1.16 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.763 μs |  0.1936 μs | 0.0106 μs |  0.33 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.342 μs |  0.1433 μs | 0.0079 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.904 μs |  0.5843 μs | 0.0320 μs |  0.54 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.744 μs |  0.0861 μs | 0.0047 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 344.008 μs | 18.9785 μs | 1.0403 μs | 23.63 |    0.35 |    6 | 17.0898 | 0.4883 |  291296 B |       69.01 |
