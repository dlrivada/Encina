```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.504 μs |  1.6811 μs | 0.0921 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4096 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.513 μs |  3.0885 μs | 0.1693 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4168 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.627 μs |  0.1479 μs | 0.0081 μs |  0.27 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.177 μs |  0.5006 μs | 0.0274 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.570 μs |  0.1847 μs | 0.0101 μs |  0.49 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.814 μs |  0.0635 μs | 0.0035 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 339.699 μs | 47.0401 μs | 2.5784 μs | 25.16 |    0.22 |    6 | 17.0898 | 0.4883 |  291296 B |       71.12 |
