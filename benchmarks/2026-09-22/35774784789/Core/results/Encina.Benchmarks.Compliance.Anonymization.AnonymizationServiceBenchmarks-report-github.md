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
| &#39;Tokenize: UUID format&#39;                   |  14.036 μs |  2.4836 μs | 0.1361 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.1068 |    4218 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.248 μs |  1.6179 μs | 0.0887 μs |  1.02 |    0.01 |    5 |  0.2441 | 0.1221 |    4792 B |        1.14 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.689 μs |  0.2243 μs | 0.0123 μs |  0.33 |    0.00 |    3 |  0.0305 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.281 μs |  0.0860 μs | 0.0047 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.593 μs |  0.1642 μs | 0.0090 μs |  0.54 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.683 μs |  0.1188 μs | 0.0065 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 348.915 μs | 17.4665 μs | 0.9574 μs | 24.86 |    0.22 |    6 | 17.0898 | 0.4883 |  291296 B |       69.06 |
