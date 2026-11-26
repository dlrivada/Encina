```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.074 μs |  3.6135 μs | 0.1981 μs |  1.00 |    0.02 |    5 |  0.2594 | 0.1373 |    4687 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.031 μs |  1.6438 μs | 0.0901 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4168 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.780 μs |  0.0778 μs | 0.0043 μs |  0.25 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.546 μs |  0.7408 μs | 0.0406 μs |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4.839 μs |  0.2319 μs | 0.0127 μs |  0.44 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.134 μs |  0.1801 μs | 0.0099 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 245.739 μs | 16.6938 μs | 0.9150 μs | 22.19 |    0.35 |    6 | 17.0898 | 0.4883 |  291296 B |       62.15 |
