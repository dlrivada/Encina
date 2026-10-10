```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|-------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9,865.1 ns |   1,381.6 ns |    75.73 ns |  1.00 |    0.01 |    5 |  0.2594 | 0.1221 |    4813 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10,026.3 ns |   9,852.0 ns |   540.02 ns |  1.02 |    0.05 |    5 |  0.2136 | 0.0763 |    3688 B |        0.77 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,808.0 ns |   1,500.6 ns |    82.25 ns |  0.28 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,448.3 ns |     179.9 ns |     9.86 ns |  0.15 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,546.4 ns |     115.8 ns |     6.35 ns |  0.46 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.23 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     801.8 ns |     101.2 ns |     5.55 ns |  0.08 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.19 |
| &#39;Risk assessment: 100-record dataset&#39;     | 196,638.6 ns | 135,942.0 ns | 7,451.44 ns | 19.93 |    0.67 |    6 | 17.3340 | 0.4883 |  291296 B |       60.52 |
