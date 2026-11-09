```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.178 μs |  3.6029 μs | 0.1975 μs |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4093 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  12.823 μs |  1.0408 μs | 0.0570 μs |  0.97 |    0.01 |    5 |  0.2289 | 0.0916 |    4162 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.676 μs |  0.2106 μs | 0.0115 μs |  0.28 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.088 μs |  0.0944 μs | 0.0052 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.525 μs |  0.1013 μs | 0.0056 μs |  0.50 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.709 μs |  0.1273 μs | 0.0070 μs |  0.13 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 339.110 μs | 11.0743 μs | 0.6070 μs | 25.74 |    0.34 |    6 | 17.0898 | 0.4883 |  291296 B |       71.17 |
