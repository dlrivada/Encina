```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  13.945 μs |  0.7854 μs | 0.0430 μs |  1.00 |    0.00 |    5 |  0.2289 | 0.0916 |    4090 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  13.990 μs |  1.0969 μs | 0.0601 μs |  1.00 |    0.00 |    5 |  0.2289 | 0.0916 |    4164 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   4.724 μs |  0.3577 μs | 0.0196 μs |  0.34 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   2.389 μs |  0.2072 μs | 0.0114 μs |  0.17 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   7.819 μs |  0.7835 μs | 0.0429 μs |  0.56 |    0.00 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.723 μs |  0.2032 μs | 0.0111 μs |  0.12 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 331.380 μs | 35.3071 μs | 1.9353 μs | 23.76 |    0.14 |    6 | 17.0898 | 0.4883 |  291296 B |       71.22 |
