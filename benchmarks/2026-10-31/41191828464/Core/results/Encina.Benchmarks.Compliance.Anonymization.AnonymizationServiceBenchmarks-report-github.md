```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  14.388 μs |  0.9031 μs | 0.0495 μs |  1.00 |    0.00 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  14.345 μs |  3.5923 μs | 0.1969 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4168 B |        1.19 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.326 μs |  0.3700 μs | 0.0203 μs |  0.30 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.312 μs |  0.0479 μs | 0.0026 μs |  0.16 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.616 μs |  0.5516 μs | 0.0302 μs |  0.53 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.735 μs |  0.0377 μs | 0.0021 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 330.908 μs | 29.4704 μs | 1.6154 μs | 23.00 |    0.12 |    6 | 17.0898 | 0.4883 |  291296 B |       83.32 |
