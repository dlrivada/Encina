```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |  11.265 μs |  1.2012 μs | 0.0658 μs |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4225 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  11.542 μs |  1.6704 μs | 0.0916 μs |  1.02 |    0.01 |    5 |  0.2441 | 0.1068 |    4288 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   3.384 μs |  0.0774 μs | 0.0042 μs |  0.30 |    0.00 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1.709 μs |  0.0382 μs | 0.0021 μs |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   5.695 μs |  0.1455 μs | 0.0080 μs |  0.51 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |   1.125 μs |  0.0221 μs | 0.0012 μs |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 245.099 μs | 34.8754 μs | 1.9116 μs | 21.76 |    0.18 |    6 | 17.3340 | 0.4883 |  291296 B |       68.95 |
