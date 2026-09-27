```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,846.3 ns |  1,872.7 ns |   102.65 ns |  1.00 |    0.01 |    5 |  0.2594 | 0.1221 |    4823 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,695.6 ns |    871.7 ns |    47.78 ns |  0.98 |    0.01 |    5 |  0.2441 | 0.0916 |    4290 B |        0.89 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,703.4 ns |  2,370.5 ns |   129.94 ns |  0.31 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,438.3 ns |    263.6 ns |    14.45 ns |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,605.0 ns |    427.1 ns |    23.41 ns |  0.52 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     790.5 ns |    193.3 ns |    10.60 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.19 |
| &#39;Risk assessment: 100-record dataset&#39;     | 189,608.9 ns | 24,610.8 ns | 1,349.00 ns | 21.44 |    0.25 |    6 | 17.3340 | 0.4883 |  291296 B |       60.40 |
