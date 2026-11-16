```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.811 μs |  1.2818 μs | 0.0703 μs |  1.00 |    0.01 |    5 |  0.2594 | 0.1068 |    4690 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.773 μs |  1.1661 μs | 0.0639 μs |  1.00 |    0.01 |    5 |  0.1984 | 0.0610 |    3568 B |        0.76 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2.796 μs |  0.1054 μs | 0.0058 μs |  0.26 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.571 μs |  0.0834 μs | 0.0046 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.270 μs |  0.3462 μs | 0.0190 μs |  0.49 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.093 μs |  0.1032 μs | 0.0057 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 243.151 μs | 18.7118 μs | 1.0257 μs | 22.49 |    0.15 |    6 | 17.3340 | 0.4883 |  291296 B |       62.11 |
