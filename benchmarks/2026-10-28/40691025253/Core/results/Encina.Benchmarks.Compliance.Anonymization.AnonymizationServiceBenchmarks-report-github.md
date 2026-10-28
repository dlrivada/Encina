```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  10.982 μs |  2.2268 μs | 0.1221 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4083 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.181 μs |  1.8593 μs | 0.1019 μs |  1.02 |    0.01 |    5 |  0.2289 | 0.0916 |    4169 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.366 μs |  0.6326 μs | 0.0347 μs |  0.31 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.728 μs |  0.1938 μs | 0.0106 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   6.119 μs |  0.2825 μs | 0.0155 μs |  0.56 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.092 μs |  0.2033 μs | 0.0111 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 246.815 μs | 11.7034 μs | 0.6415 μs | 22.48 |    0.22 |    6 | 17.0898 | 0.4883 |  291296 B |       71.34 |
