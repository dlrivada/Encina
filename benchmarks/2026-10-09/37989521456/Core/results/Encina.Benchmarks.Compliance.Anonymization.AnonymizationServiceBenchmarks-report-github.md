```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.796 μs |  1.3454 μs | 0.0737 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0763 |    4225 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10.929 μs |  1.2794 μs | 0.0701 μs |  1.01 |    0.01 |    5 |  0.2594 | 0.1068 |    4891 B |        1.16 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.416 μs |  0.0958 μs | 0.0053 μs |  0.32 |    0.00 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.789 μs |  0.0675 μs | 0.0037 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.777 μs |  0.3186 μs | 0.0175 μs |  0.54 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.083 μs |  0.0977 μs | 0.0054 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 245.532 μs | 29.8362 μs | 1.6354 μs | 22.74 |    0.19 |    6 | 17.3340 | 0.4883 |  291296 B |       68.95 |
